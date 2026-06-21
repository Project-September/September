using System.Collections.Generic;
using System.Linq;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Splines;

namespace September.InGame.Kraken
{
    public class IKFollower : MonoBehaviour
    {
        [SerializeField] private IK _ik;
        [SerializeField] private Rigidbody[] _followers;
        [SerializeField] private float _radius = .55f;

        [SerializeField, HideInInspector] private List<float> _maxDistanceList = new();
        [SerializeField, HideInInspector] private Quaternion[] _defaultRotations;

        private struct Point
        {
            public Vector3 position;
            public Quaternion rotation;

            public Point(Vector3 position, Quaternion rotation)
            {
                this.position = position;
                this.rotation = rotation;
            }

            public static implicit operator Vector3(Point point) => point.position;
        }

        private void Start()
        {
            _maxDistanceList.Add(0);
            for (int i = 0; i < _followers.Length - 1; i++)
            {
                var p0 = _followers[i].position;
                var p1 = _followers[i + 1].position;

                _maxDistanceList.Add((p1 - p0).magnitude);
            }

            _defaultRotations = _followers.Select(x => x.rotation).ToArray();
        }

        private void Update()
        {
            IKSolver solver = _ik.GetIKSolver();
            DebugDrawLine(solver.GetPoints().Select(x => x.solverPosition).ToArray(), Color.yellow);
            DebugDrawSpheres(solver.GetPoints().Select(x => x.solverPosition).ToArray(), .2f, Color.yellow);

            CheckHit(_followers, solver, _radius, out Vector3[] rawPoints, out bool[] rigidbodyHitFlags);

            DebugDrawLine(rawPoints, Color.green);

            var splinePoints = rawPoints.Where((_, i) => i == 0 || i == rawPoints.Length - 1 || rigidbodyHitFlags[i]).ToArray();
            Spline resultSpline = CreateSpline(splinePoints);

            DebugDrawSpline(resultSpline, Color.blue);

            int hitCount = -1;
            for (int i = rigidbodyHitFlags.Length - 1; i >= 0; i--)
            {
                if (rigidbodyHitFlags[i])
                {
                    hitCount = i + 1;
                    break;
                }
            }

            var hullPoints = BuildUpperHull(rawPoints.Take(hitCount).ToArray());
            DebugDrawSpheres(hullPoints, .2f, Color.magenta);
            DebugDrawLine(hullPoints, Color.magenta);

            if (!rigidbodyHitFlags.Any(x => x))
            {
                UpdatePosition(GetPoints(resultSpline));
                return;
            }

            var notCollidedPoint = rawPoints.Skip(hitCount).ToArray();
            DebugDrawSpheres(notCollidedPoint, _radius-.1f, Color.green);

            var resultPoints = hullPoints.Concat(rawPoints.Skip(hitCount)).ToArray();

            var hullSpline = CreateSpline(resultPoints);
            var hullSolvedPoints = GetPoints(hullSpline);

            UpdatePosition(hullSolvedPoints);
            DebugDrawSpheres(hullSolvedPoints, _radius-.1f, Color.magenta);

            var correctedPoints = new Vector3[hullSolvedPoints.Length];
            for (int i = 0; i < hullSolvedPoints.Length; i++)
            {
                correctedPoints[i] = CorrectTargetPosition(hullSolvedPoints[i], _followers[i], _followers, _radius);
            }
            var correctedSpline = CreateSpline(correctedPoints);
            var finalPoints = GetPoints(correctedSpline);

            DebugDrawLine(finalPoints, Color.cyan);
        }

        private static void CheckHit(Rigidbody[] followerBodies, IKSolver solver, float radius, out Vector3[] rawPoints, out bool[] rigidbodyHitFlags)
        {
            var ikPoints = solver.GetPoints();

            rigidbodyHitFlags = new bool[followerBodies.Length];
            rawPoints = new Vector3[ikPoints.Length];

            for (int i = 0; i < ikPoints.Length; i++)
            {
                if (followerBodies.Length < i || followerBodies[i] == null) continue;

                var offset = ikPoints[i].solverPosition - followerBodies[i].position;
                var results = followerBodies[i].SweepTestAll(offset, offset.magnitude);

                var nearestHitInfo = results
                    .OrderBy(x => (x.point - followerBodies[i].position).sqrMagnitude)
                    .FirstOrDefault(x => !followerBodies.Contains(x.rigidbody));

                if (nearestHitInfo.collider == null)
                {
                    rawPoints[i] = ikPoints[i].solverPosition;
                    continue;
                }

                rigidbodyHitFlags[i] = true;

                if (Physics.ComputePenetration(
                        nearestHitInfo.collider, nearestHitInfo.transform.position, nearestHitInfo.collider.transform.rotation,
                        followerBodies[i].GetComponent<Collider>(), followerBodies[i].position, followerBodies[i].transform.rotation,
                        out var dir, out var distance))
                {
                    rawPoints[i] = followerBodies[i].position + dir * (distance + .1f);
                }
                else
                {
                    rawPoints[i] = nearestHitInfo.point + nearestHitInfo.normal * radius;
                }
            }
        }

        private static Spline CreateSpline(IReadOnlyList<Vector3> points)
        {
            var spline = new Spline();
            foreach (var point in points)
            {
                spline.Add(point, TangentMode.Broken);
            }
            return spline;
        }

        private void UpdatePosition(Point[] points)
        {
            for (int i = 0; i < _followers.Length; i++)
            {
                if (_followers[i] == null) continue;

                _followers[i].MovePosition(points[i].position);
                _followers[i].MoveRotation(points[i].rotation * _defaultRotations[i]);
            }
        }

        private Point[] GetPoints(Spline spline)
        {
            var results = new Point[_followers.Length];

            var distancePrefixSum = new List<float>();
            for (int i = 0; i < _maxDistanceList.Count; i++)
            {
                distancePrefixSum.Add(_maxDistanceList.Take(i + 1).Sum());
            }

            for (int i = 0; i < _followers.Length; i++)
            {
                if (_followers[i] == null) continue;

                var curveLength = spline.GetLength();
                var targetLength = distancePrefixSum[i];
                var t = targetLength / curveLength;
                spline.Evaluate(t, out var position, out var tangent, out var upVector);

                Debug.DrawRay(position, tangent, Color.red);
                Debug.DrawRay(position, upVector, Color.green);

                var rotation = Quaternion.LookRotation(tangent, upVector);

                results[i] = new Point(position, rotation);
            }

            return results;
        }

        private static Vector3 CorrectTargetPosition(Vector3 originalPosition, Rigidbody target, Rigidbody[] followerBodies, float radius)
        {
            var overlaps = Physics.OverlapSphere(originalPosition, radius)
                .Where(x => !followerBodies.Contains(x.GetComponent<Rigidbody>()))
                .ToArray();

            foreach (Collider overlap in overlaps)
            {
                if (Physics.ComputePenetration(
                        target.GetComponent<Collider>(), originalPosition, target.transform.rotation,
                        overlap, overlap.transform.position, overlap.transform.rotation,
                        out Vector3 dir, out float distance))
                {
                    DebugDrawUtility.DrawWireSphere(originalPosition, .2f, Color.red);
                    Debug.DrawRay(originalPosition, dir * distance, Color.red);
                    DebugDrawUtility.DrawWireSphere(originalPosition + dir * (distance + .1f), radius, Color.red);
                    return originalPosition + dir * (distance + .1f);
                }
            }

            return originalPosition;
        }

        #region ConvexHull

        // AI生成
        // Monotone Chainらしい
        struct HullPoint
        {
            public float u;
            public float y;
            public Vector3 position;
        }

        private static List<Vector3> BuildUpperHull(
            IReadOnlyList<Vector3> points)
        {
            if (points.Count <= 2)
                return new List<Vector3>(points);

            Vector3 horizontal =
                Vector3.ProjectOnPlane(
                    points[points.Count - 1] - points[0],
                    Vector3.up);

            if (horizontal.sqrMagnitude < 0.000001f)
            {
                for (int i = 1; i < points.Count; i++)
                {
                    horizontal =
                        Vector3.ProjectOnPlane(
                            points[i] - points[0],
                            Vector3.up);

                    if (horizontal.sqrMagnitude > 0.000001f)
                        break;
                }
            }

            horizontal.Normalize();

            List<HullPoint> projected = new();

            foreach (var p in points)
            {
                projected.Add(new HullPoint
                {
                    u = Vector3.Dot(p, horizontal),
                    y = p.y,
                    position = p
                });
            }

            projected.Sort((a, b) => a.u.CompareTo(b.u));

            List<HullPoint> upper = new();

            foreach (var p in projected)
            {
                while (upper.Count >= 2)
                {
                    var a = upper[upper.Count - 2];
                    var b = upper[upper.Count - 1];

                    float cross =
                        (b.u - a.u) * (p.y - a.y) -
                        (b.y - a.y) * (p.u - a.u);

                    if (cross >= 0f)
                        upper.RemoveAt(upper.Count - 1);
                    else
                        break;
                }

                upper.Add(p);
            }

            List<Vector3> result = new(upper.Count);

            foreach (var p in upper)
                result.Add(p.position);

            return result;
        }
        #endregion

        #region Debug
        private static void DebugDrawSpline(Spline spline, Color color)
        {
            for (int i = 0; i <= 50; i++)
            {
                spline.Evaluate(i / 50f, out var position, out var tangent, out var normal);
                spline.Evaluate((i + 1) / 50f, out var endPosition, out _, out _);
                Debug.DrawLine(position, endPosition, color);
            }
        }

        private void DebugDrawLine(IReadOnlyList<Vector3> points, Color color)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p0 = points[i];
                Vector3 p1 = points[i + 1];
                Debug.DrawLine(p0, p1, color);
            }
        }

        private void DebugDrawLine(IReadOnlyList<Point> points, Color color)
        {
            DebugDrawLine(ConvertToVector(points), color);
        }

        private void DebugDrawSpheres(IReadOnlyList<Vector3> points, float radius, Color color)
        {
            foreach (var p in points)
            {
                DebugDrawUtility.DrawWireSphere(p, radius, color);
            }
        }

        private void DebugDrawSpheres(IReadOnlyList<Point> points, float radius, Color color)
        {
            DebugDrawSpheres(ConvertToVector(points), radius, color);
        }

        private Vector3[] ConvertToVector(IReadOnlyList<Point> points)
        {
            return points.Select(x => x.position).ToArray();
        }
        #endregion
    }
}
