using System.Collections.Generic;
using System.Linq;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Splines;

namespace September.InGame.Kraken
{
    public class IKFollower : MonoBehaviour
    {
        [SerializeField] private FABRIK _ik;
        [SerializeField] private Rigidbody[] _followers;
        [SerializeField] private float _radius = .55f;

        private readonly List<float> _maxDistanceList = new();

        private void Start()
        {
            _maxDistanceList.Add(0);
            for (int i = 0; i < _followers.Length - 1; i++)
            {
                var p0 = _followers[i].position;
                var p1 = _followers[i + 1].position;

                _maxDistanceList.Add((p1 - p0).magnitude);
            }
        }

        private void Update()
        {
            for (int i = 0; i < _ik.solver.GetPoints().Length - 1; i++)
            {
                Debug.DrawLine(_ik.solver.GetPoints()[i].solverPosition, _ik.solver.GetPoints()[i + 1].solverPosition, Color.yellow);
            }

            CheckHit(_followers, _ik, _radius, out var rawPoints, out var resultSpline, out var rigidbodyHitFlags);

            for (int i = 0; i < rawPoints.Length - 1; i++)
            {
                var p0 = rawPoints[i];
                var p1 = rawPoints[i + 1];
                Debug.DrawLine(p0, p1, Color.green);
            }

            for (int i = 0; i <= 50; i++)
            {
                resultSpline.Evaluate(i / 50f, out var position, out var tangent, out var normal);
                resultSpline.Evaluate((i + 1) / 50f, out var endPosition, out _, out _);
                Debug.DrawLine(position, endPosition, Color.blue);
            }

            if (!rigidbodyHitFlags.Any(x => x))
            {
                UpdatePosition(rawPoints);
                return;
            }

            UpdatePosition(rawPoints);
        }

        private static void CheckHit(Rigidbody[] followerBodies, FABRIK ik, float radius, out Vector3[] rawPoints, out Spline resultSpline, out bool[] rigidbodyHitFlags)
        {
            var ikPoints = ik.solver.GetPoints();

            rigidbodyHitFlags = new bool[followerBodies.Length];
            rawPoints = new Vector3[ikPoints.Length];
            resultSpline = new Spline();
            resultSpline.Add(followerBodies[0].position);

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

                resultSpline.Add(rawPoints[i]);
            }

            resultSpline.Add(rawPoints[^1]);
        }

        private void UpdatePosition(Vector3[] rawPoints)
        {
            for (int i = 0; i < _followers.Length; i++)
            {
                if (_followers[i] == null) continue;

                _followers[i].MovePosition(rawPoints[i]);
            }
        }

        private void UpdatePosition(Spline spline)
        {
            // Attach Curve to Positions
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
                spline.Evaluate(t, out var position, out _, out _);

                DebugDrawUtility.DrawWireSphere(position, .3f, Color.red);

                _followers[i].MovePosition(position);
            }
        }
    }
}
