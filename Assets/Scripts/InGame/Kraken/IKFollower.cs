using System.Collections.Generic;
using System.Linq;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.Splines;

namespace September.InGame.Kraken
{
    public class IKFollower : MonoBehaviour
    {
        private readonly struct Point
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;

            public Point(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            public static Vector3[] ConvertToVector(IReadOnlyList<Point> points)
            {
                return points.Select(x => x.Position).ToArray();
            }
        }

        [SerializeField] private IK _ik;
        [SerializeField] private Rigidbody[] _followers;
        [SerializeField] private float _radius = .55f;

        [SerializeField, HideInInspector] private List<float> _maxDistanceList = new();
        [SerializeField, HideInInspector] private Quaternion[] _defaultRotations;

        private void Start()
        {
            // ボーンの長さをキャッシュ
            _maxDistanceList.Add(0);
            for (int i = 0; i < _followers.Length - 1; i++)
            {
                var p0 = _followers[i].position;
                var p1 = _followers[i + 1].position;

                _maxDistanceList.Add((p1 - p0).magnitude);
            }

            // ボーンの回転をキャッシュ
            _defaultRotations = _followers.Select(x => x.rotation).ToArray();
        }

        // Todo: 船に沿わせるための処理
        // Todo: スイープ後の回転を安定させる
        // Todo: スイープが貫通する問題を修正する
        private void Update()
        {
            // 0. FABRIKで障害物がない時のボーン位置を計算
            IKSolver solver = _ik.GetIKSolver();
            DebugDrawLine(solver.GetPoints().Select(x => x.solverPosition).ToArray(), Color.yellow);
            DebugDrawSpheres(solver.GetPoints().Select(x => x.solverPosition).ToArray(), _radius * .2f, Color.yellow);

            // 1. スイープ移動した位置を計算
            CalcSweptPosition(_followers, solver, _radius, out Vector3[] rawPoints, out bool[] rigidbodyHitFlags);
            DebugDrawLine(rawPoints, Color.green);

            var splinePoints = rawPoints.Where((_, i) => i == 0 || i == rawPoints.Length - 1 || rigidbodyHitFlags[i]).ToArray();
            Spline resultSpline = CreateSpline(splinePoints);

            DebugDrawSpline(resultSpline, Color.blue);

            // 2. 衝突がなければそのままにする
            if (!rigidbodyHitFlags.Any(x => x))
            {
                UpdatePosition(GetPoints(resultSpline));
                return;
            }

            // 3. 衝突のあるセクションを凸包に変換
            int hitCount = -1;
            for (int i = rigidbodyHitFlags.Length - 1; i >= 0; i--)
            {
                if (rigidbodyHitFlags[i])
                {
                    hitCount = i + 1;
                    break;
                }
            }

            var hullPoints = ConvexHull.BuildUpperHull(rawPoints.Take(hitCount).ToArray());
            DebugDrawSpheres(hullPoints, _radius * .2f, Color.magenta);
            DebugDrawLine(hullPoints, Color.magenta);

            // 4. 衝突のないセクションと結合する
            var notCollidedPoint = rawPoints.Skip(hitCount).ToArray();
            DebugDrawSpheres(notCollidedPoint, _radius * .9f, Color.red);

            var resultPoints = hullPoints.Concat(rawPoints.Skip(hitCount)).ToArray();

            var hullSpline = CreateSpline(resultPoints);
            var hullSolvedPoints = GetPoints(hullSpline);

            UpdatePosition(hullSolvedPoints);
            DebugDrawSpheres(hullSolvedPoints, _radius * .9f, Color.Lerp(Color.red, Color.yellow, 0.5f));

            // 5. 埋まりを修正する
            var correctedPoints = new Vector3[hullSolvedPoints.Length];
            for (int i = 0; i < hullSolvedPoints.Length; i++)
            {
                correctedPoints[i] = CorrectTargetPosition(hullSolvedPoints[i].Position, _followers[i], _followers, _radius);
            }
            var correctedSpline = CreateSpline(correctedPoints);
            var finalPoints = GetPoints(correctedSpline);

            DebugDrawSpheres(finalPoints, _radius * .9f, Color.cyan);
            DebugDrawLine(finalPoints, Color.cyan);
        }

        private static void CalcSweptPosition(Rigidbody[] followerBodies, IKSolver solver, float radius, out Vector3[] rawPoints, out bool[] rigidbodyHitFlags)
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

        #region Common

        private static Spline CreateSpline(IReadOnlyList<Vector3> points)
        {
            var spline = new Spline();
            foreach (var point in points)
            {
                spline.Add(point, TangentMode.Broken);
            }
            return spline;
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

                Debug.DrawRay(position, upVector, Color.green);

                if (Vector3.SqrMagnitude(tangent) > 0f && Vector3.SqrMagnitude(upVector) > 0f)
                {
                    var rotation = Quaternion.LookRotation(tangent, upVector);
                    results[i] = new Point(position, rotation);
                }
                else
                {
                    results[i] = new Point(position, Quaternion.identity);
                }
            }

            return results;
        }

        private void UpdatePosition(Point[] points)
        {
            for (int i = 0; i < _followers.Length; i++)
            {
                if (_followers[i] == null) continue;

                _followers[i].MovePosition(points[i].Position);
                _followers[i].MoveRotation(points[i].Rotation * _defaultRotations[i]);
            }
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
            DebugDrawLine(Point.ConvertToVector(points), color);
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
            DebugDrawSpheres(Point.ConvertToVector(points), radius, color);
        }
        #endregion
    }

    public static class ConvexHull
    {
        // AI生成
        // Monotone Chainらしい
        struct HullPoint
        {
            public float u;
            public float y;
            public Vector3 position;
        }

        public static List<Vector3> BuildUpperHull(IReadOnlyList<Vector3> points)
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
    }
}
