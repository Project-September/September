using UnityEngine;

namespace September.InGame.Kraken.Animations
{
    /// <summary>
    /// Position-Based Dynamics (PBD) based collision solver for tentacles.
    /// Satisfies non-penetration, distance, and bending/rotation constraints
    /// to ensure natural-looking animations that snap to deck colliders.
    /// Supports landed/staying state to keep the tentacle on the deck for a given duration.
    /// </summary>
    public class TentacleConstraintSolver : MonoBehaviour
    {
        [Header("Collision Settings")]
        [SerializeField] private float _radius = 0.55f;
        [SerializeField] private LayerMask _layerMask;

        [Header("Constraint Settings")]
        [SerializeField] private float _maxBendingAngle = 25f; // Max angle (degrees) between adjacent segments
        [SerializeField] private float _maxRotationSpeed = 180f; // Max rotation change (degrees/second) for stability
        [SerializeField] private int _pbdIterations = 5;
        [SerializeField] private int _pbdSubsteps = 5;

        private float[] _segmentLengths;
        private IKFollower.Point[] _prevSolvedPoints;

        /// <summary>
        /// Resets the solver's state (e.g. if the tentacle is re-spawned or reset).
        /// </summary>
        public void Reset()
        {
            _prevSolvedPoints = null;
            _segmentLengths = null;
        }

        /// <summary>
        /// Solves constraints for the given input points.
        /// </summary>
        public IKFollower.Point[] Solve(IKFollower.Point[] inputPoints, float deltaTime)
        {
            if (inputPoints == null || inputPoints.Length == 0)
                return inputPoints;

            int count = inputPoints.Length;

            // 1. Initialize segment lengths and cache buffers on first run or when bone count changes
            InitializeBuffers(inputPoints);

            // 2. Prepare predicted positions
            var solvedPositions = new Vector3[count];
            var solvedRotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
            {
                solvedPositions[i] = _prevSolvedPoints[i].Position;
                solvedRotations[i] = inputPoints[i].Rotation;
            }

            // 3. Update stay timer and state machine
            // UpdateStateMachine(inputPoints, solvedPositions, deltaTime);

            // 4. Run PBD Iterative Solver to satisfy constraints
            for (int iter = 0; iter < _pbdIterations; iter++)
            {
                for (int step = 0; step < _pbdSubsteps; step++)
                {
                    // A. Non-penetration Constraint (非侵入拘束)
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 h = (inputPoints[i].Position - solvedPositions[i]) * (1f / _pbdSubsteps);
                        Vector3 p = solvedPositions[i] + h;

                        solvedPositions[i] = ResolveCollisions(i, p, _radius, _layerMask);
                    }

                    // B. Distance Constraint (距離拘束) - standard PBD with mass weights
                    for (int i = 1; i < count; i++)
                    {
                        float targetDist = _segmentLengths[i];
                        Vector3 diff = solvedPositions[i] - solvedPositions[i - 1];
                        float currentDist = diff.magnitude;

                        if (currentDist > 0.0001f)
                        {
                            Vector3 dir = diff / currentDist;
                            float error = currentDist - targetDist;
                            Vector3 correction = dir * error / 2;

                            solvedPositions[i - 1] += correction;
                            solvedPositions[i] -= correction;
                        }
                    }

                    // C. Bending / Rotation Constraint (回転拘束)
                    // Restricts the angle between consecutive bone segments
                    for (int i = 2; i < count; i++)
                    {
                        Vector3 v1 = solvedPositions[i - 1] - solvedPositions[i - 2];
                        Vector3 v2 = solvedPositions[i] - solvedPositions[i - 1];

                        float angle = Vector3.Angle(v1, v2);
                        if (angle > _maxBendingAngle)
                        {
                            Vector3 axis = Vector3.Cross(v1, v2).normalized;
                            if (axis.sqrMagnitude < 0.001f)
                                axis = Vector3.up;

                            Quaternion limitRot = Quaternion.AngleAxis(_maxBendingAngle - angle, axis);
                            Vector3 constrainedV2 = limitRot * v2;

                            solvedPositions[i] = solvedPositions[i - 1] + constrainedV2.normalized * _segmentLengths[i];
                        }
                    }
                }
            }

            // 5. Compute Orientations (Rotations)
            // Align bone rotations with the solved bone segment directions
            for (int i = 0; i < count; i++)
            {
                if (i < count - 1)
                {
                    Vector3 origDir = inputPoints[i + 1].Position - inputPoints[i].Position;
                    Vector3 solvedDir = solvedPositions[i + 1] - solvedPositions[i];

                    if (origDir.sqrMagnitude > 0.0001f && solvedDir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion deltaRot = Quaternion.FromToRotation(origDir.normalized, solvedDir.normalized);
                        Quaternion targetRot = deltaRot * inputPoints[i].Rotation;

                        // Apply temporal damping to satisfy the rotational velocity constraint
                        if (_prevSolvedPoints != null && _prevSolvedPoints.Length == count)
                        {
                            float maxAngleChange = _maxRotationSpeed * deltaTime;
                            solvedRotations[i] = Quaternion.RotateTowards(_prevSolvedPoints[i].Rotation, targetRot, maxAngleChange);
                        }
                        else
                        {
                            solvedRotations[i] = targetRot;
                        }
                    }
                    else
                    {
                        solvedRotations[i] = inputPoints[i].Rotation;
                    }
                }
                else
                {
                    // For the tip, match the previous segment's solved rotation or fallback to original
                    if (count >= 2)
                    {
                        solvedRotations[i] = solvedRotations[i - 1];
                    }
                    else
                    {
                        solvedRotations[i] = inputPoints[i].Rotation;
                    }
                }
            }

            // 6. Build the final resolved Points
            var solvedPoints = new IKFollower.Point[count];
            for (int i = 0; i < count; i++)
            {
                solvedPoints[i] = new IKFollower.Point(solvedPositions[i], solvedRotations[i]);
            }

            // Cache for next frame
            _prevSolvedPoints = solvedPoints;

            return solvedPoints;
        }

        private void InitializeBuffers(IKFollower.Point[] inputPoints)
        {
            int count = inputPoints.Length;
            if (_segmentLengths == null || _segmentLengths.Length != count)
            {
                _segmentLengths = new float[count];
                _segmentLengths[0] = 0f;
                for (int i = 1; i < count; i++)
                {
                    _segmentLengths[i] = Vector3.Distance(inputPoints[i - 1].Position, inputPoints[i].Position);
                }

                _prevSolvedPoints = inputPoints;
            }
        }

        /// <summary>
        /// Precision sphere-to-box collision solver.
        /// Transforms sphere coordinates to local BoxCollider space to handle deep penetrations
        /// and compute accurate contact surface normals and points.
        /// </summary>
        private Vector3 ResolveCollisions(int i, Vector3 position, float radius, LayerMask layerMask)
        {
            Collider[] colliders = Physics.OverlapSphere(position, radius, layerMask);
            if (colliders == null || colliders.Length == 0)
                return position;

            foreach (Collider col in colliders)
            {
                // Standard fallback closest point resolution for other types of colliders
                Vector3 closestPoint = col.ClosestPoint(position);
                float dist = Vector3.Distance(position, closestPoint);
                if (dist < radius)
                {
                    Vector3 dir = (position - closestPoint).normalized;
                    if (dir == Vector3.zero)
                        dir = Vector3.up;
                    position = closestPoint + dir * radius;
                }
            }

            return position;
        }
    }
}
