using System.Collections.Generic;
using System.Linq;
using RootMotion.FinalIK;
using September.Common.Extensions;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace September.InGame.Kraken
{
    public class IKFollower : MonoBehaviour
    {
        public readonly struct Point
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;

            public Point(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            public Point(Rigidbody rigidbody)
            {
                Position = rigidbody.position;
                Rotation = rigidbody.rotation;
            }

            public Point(IKSolver.Point point)
            {
                Position = point.solverPosition;
                Rotation = point.solverRotation;
            }

            public static Vector3[] ConvertToVector(IReadOnlyList<Point> points)
            {
                return points.Select(x => x.Position).ToArray();
            }

            public static Quaternion GetInterpolatedRotation(Point p0, Point p1, Vector3 target)
            {
                Vector3 v0 = target - p0.Position;
                Vector3 v1 = target - p1.Position;

                float d0 = v0.magnitude;
                float d1 = v1.magnitude;

                // d0 = 0 => 0
                // d1 = 0 => 1
                // d0 = d1 => .5
                float m = Mathf.Max(d0, d1);

                float ratio = ((d0 - d1) / m + 1f) * 0.5f;

                return Quaternion.Slerp(p0.Rotation, p1.Rotation, ratio);
            }
        }

        [SerializeField] private IK _ik;
        [SerializeField] private Rigidbody[] _followers;
        [SerializeField] private float _radius = .55f;

        [SerializeField, HideInInspector] private List<float> _maxDistanceList = new();
        [SerializeField, HideInInspector] private Quaternion[] _defaultRotations;

        [Header("Debug Settings")]
        [SerializeField] private bool _debugFabrikPoints;
        [SerializeField] private bool _debugFabrikUpward;
        [SerializeField] private bool _debugSweptPoints;
        [SerializeField] private bool _debugSweptUpward;
        [SerializeField] private bool _debugHittedSpline;
        [SerializeField] private bool _debugHullPoints;
        [SerializeField] private bool _debugHullUpward;
        [SerializeField] private bool _debugNotCollided;
        [SerializeField] private bool _debugConcat;
        [SerializeField] private bool _debugCorrected;

        private void Start()
        {
            // ボーンの長さをキャッシュ
            _maxDistanceList.Add(0);
            for (int i = 0; i < _followers.Length - 1; i++)
            {
                var p0 = _followers[i].position;
                var p1 = _followers[i + 1].position;

                _maxDistanceList.Add((p1 - p0).magnitude);

                if (i != 0)
                {
                    _maxDistanceList[i] += _maxDistanceList[i - 1];
                }
            }

            // ボーンの回転をキャッシュ
            _defaultRotations = _followers.Select(x => x.rotation).ToArray();

            Validate();
        }

        private void Validate()
        {
            Debug.Assert(_maxDistanceList.Count > 0, "Max distance list is empty");

            Debug.Assert(_maxDistanceList.Skip(1).All(x => x > float.Epsilon), "Max distance element is too small");

            Debug.Assert(_defaultRotations.Length > 0, "Default rotation array is empty");
        }

        // Todo: 船に沿わせるための処理
        // Todo: スイープ後の回転を安定させる
        // Todo: スイープが貫通する問題を修正する
        private void Update()
        {
            // 0. FABRIKで障害物がない時のボーン位置を計算
            IKSolver solver = _ik.GetIKSolver();

            List<IKSolver.Point> temp = solver.GetPoints().DistinctBy(x => x.transform).ToList();

            IKSolver.Point[] points = temp.Where(x => temp.Count(y => y.transform == x.transform) == 1).ToArray();

            if (_debugFabrikPoints)
            {
                DebugDrawSpheres(points.Select(x => x.solverPosition).ToArray(), _radius * .2f, Color.yellow);
                DebugDrawLine(points.Select(x => x.solverPosition).ToArray(), Color.yellow);
            }

            if (_debugFabrikUpward)
            {
                foreach (var p in points)
                {
                    Debug.DrawRay(p.transform.position, p.transform.up, Color.yellow);
                }
            }

            // 1. スイープ移動した位置を計算
            CalcSweptPosition(_followers, points, _radius, out Point[] sweptPoints, out bool[] rigidbodyHitFlags);

            if (_debugSweptPoints)
                DebugDrawLine(sweptPoints, Color.green);

            if (_debugSweptUpward)
            {
                foreach (var p in sweptPoints)
                {
                    Debug.DrawRay(p.Position, p.Rotation * Vector3.up, Color.red);
                }
            }

            // 衝突がなければそのままにする
            if (!rigidbodyHitFlags.Any(x => x))
            {
                UpdatePosition(sweptPoints);
                return;
            }

            {
                // 衝突を考慮したスプラインを作成（未使用）
                var splinePoints = sweptPoints.Where((_, i) => i == 0 || i == sweptPoints.Length - 1 || rigidbodyHitFlags[i]).ToArray();
                IPointInterpolator resultSpline = new SplinePointInterpolator(splinePoints);

                if (_debugHittedSpline)
                    resultSpline.DebugDraw(Color.blue);
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

            int[] hullPointIndexes = ConvexHull.BuildUpperHull(sweptPoints.Take(hitCount).Select(x => x.Position).ToArray());

            Point[] hullPoints = sweptPoints.Where((_, i) => hullPointIndexes.Contains(i)).ToArray();

            if (_debugHullPoints)
            {
                DebugDrawSpheres(hullPoints, _radius * .2f, Color.magenta);
                DebugDrawLine(hullPoints, Color.magenta);
            }

            if (_debugHullUpward)
            {
                foreach (Point p in hullPoints)
                {
                    Debug.DrawRay(p.Position, p.Rotation * Vector3.up, Color.magenta);
                }
            }

            // 4. 衝突のないセクションと結合する
            var notCollidedPoint = sweptPoints.Skip(hitCount).ToArray();

            if (_debugNotCollided)
                DebugDrawSpheres(notCollidedPoint, _radius * .9f, Color.red);

            var resultPoints = hullPoints.Concat(sweptPoints.Skip(hitCount)).ToArray();

            IPointInterpolator hullSpline = new SplinePointInterpolator(resultPoints);
            Point[] hullSolvedPoints = hullSpline.Evaluate(_maxDistanceList);

            UpdatePosition(hullSolvedPoints);

            // if (_debugConcat)
            //     DebugDrawSpheres(hullSolvedPoints, _radius * .9f, Color.Lerp(Color.red, Color.yellow, 0.5f));
            //
            // // 5. 埋まりを修正する
            // var correctedPoints = new Vector3[hullSolvedPoints.Length];
            // for (int i = 0; i < hullSolvedPoints.Length; i++)
            // {
            //     correctedPoints[i] = CorrectTargetPosition(hullSolvedPoints[i].Position, _followers[i], _followers, _radius);
            // }
            // var correctedSpline = CreateSpline(correctedPoints);
            // var finalPoints = GetPoints(correctedSpline);
            //
            // if (_debugCorrected)
            // {
            //     DebugDrawSpheres(finalPoints, _radius * .9f, Color.cyan);
            //     DebugDrawLine(finalPoints, Color.cyan);
            // }
        }

        private static void CalcSweptPosition(Rigidbody[] followerBodies, IKSolver.Point[] ikPoints, float radius, out Point[] sweptPoints,
            out bool[] rigidbodyHitFlags)
        {
            rigidbodyHitFlags = new bool[followerBodies.Length];
            sweptPoints = new Point[ikPoints.Length];

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
                    sweptPoints[i] = new Point(ikPoints[i].solverPosition, ikPoints[i].solverRotation);
                    continue;
                }

                rigidbodyHitFlags[i] = true;

                Vector3 pos = nearestHitInfo.point + nearestHitInfo.normal * radius;
                if (i < ikPoints.Length - 1)
                {
                    sweptPoints[i] = new Point(pos, Point.GetInterpolatedRotation(new Point(ikPoints[i]), new Point(ikPoints[i + 1]), pos));
                }
                else
                {
                    sweptPoints[i] = new Point(pos, ikPoints[i].solverRotation);
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

        private void UpdatePosition(Point[] points)
        {
            for (int i = 0; i < _followers.Length; i++)
            {
                if (_followers[i] == null) continue;

                _followers[i].MovePosition(points[i].Position);
                _followers[i].MoveRotation(points[i].Rotation);
            }
        }

        #endregion

        #region Debug
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
            public int index;
        }

        public static int[] BuildUpperHull(IReadOnlyList<Vector3> points)
        {
            if (points.Count <= 2) return Enumerable.Range(0, points.Count).ToArray();

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

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p = points[i];
                projected.Add(new HullPoint
                {
                    u = Vector3.Dot(p, horizontal),
                    y = p.y,
                    position = p,
                    index = i
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

            return upper.Select(h => h.index).ToArray();
        }
    }

    public interface IPointInterpolator
    {
        public IKFollower.Point Evaluate(float distance);
        public IKFollower.Point[] Evaluate(IReadOnlyList<float> distances);
        public void DebugDraw(Color color);
    }

    public class SplinePointInterpolator : IPointInterpolator
    {
        private readonly Spline _spline;

        public SplinePointInterpolator(IKFollower.Point[] points)
        {
            _spline = CreateSpline(points);
        }

        private static Spline CreateSpline(IReadOnlyList<IKFollower.Point> points)
        {
            var spline = new Spline();
            foreach (var point in points)
            {
                spline.Add(new BezierKnot(point.Position, float3.zero, float3.zero, point.Rotation));
            }
            return spline;
        }

        public IKFollower.Point[] Evaluate(IReadOnlyList<float> distances)
        {
            return distances.Select(Evaluate).ToArray();
        }

        public IKFollower.Point Evaluate(float distance)
        {
            float curveLength = _spline.GetLength();
            float t = distance / curveLength;
            _spline.Evaluate(t, out var position, out var tangent, out var upVector);

            // Splineのちょうど両端の法線は必ずゼロベクトルになるっぽい
            if (Vector3.SqrMagnitude(tangent) == 0f)
            {
                _spline.Evaluate(t + .01f, out _, out tangent, out _);
                if (Vector3.SqrMagnitude(tangent) == 0f)
                {
                    _spline.Evaluate(t - .01f, out _, out tangent, out _);
                }
            }

            // TODO: tangent情報が元の姿勢を表していないためアーティファクトが発生する。
            var rotation = Quaternion.LookRotation(tangent, upVector) * Quaternion.LookRotation(Vector3.right);
            return new IKFollower.Point(position, rotation);
        }

        public void DebugDraw(Color color)
        {
            for (int i = 0; i <= 50; i++)
            {
                _spline.Evaluate(i / 50f, out var position, out var tangent, out var normal);
                _spline.Evaluate((i + 1) / 50f, out var endPosition, out _, out _);
                Debug.DrawLine(position, endPosition, color);
                Debug.DrawRay(position, normal, Color.green);
                Debug.DrawRay(position, tangent, Color.red * new Color(1f, 1f, 1f, .1f));
            }
        }
    }
}
