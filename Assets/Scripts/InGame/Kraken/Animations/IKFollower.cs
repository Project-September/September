using System.Collections.Generic;
using System.Linq;
using RootMotion.FinalIK;
using September.Common.Extensions;
using UnityEngine;

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
        [SerializeField] private LayerMask _layerMask;

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
            for (int i = 1; i < _followers.Length; i++)
            {
                var p0 = _followers[i - 1].position;
                var p1 = _followers[i].position;

                _maxDistanceList.Add((p1 - p0).magnitude);

                _maxDistanceList[i] += _maxDistanceList[i - 1];

                Debug.Log($"p0:{p0} p1:{p1} distance:{(p1 - p0).magnitude} sum:{_maxDistanceList[i]}");
            }

            // ボーンの回転をキャッシュ
            _defaultRotations = _followers.Select(x => x.rotation).ToArray();

            Validate();
        }

        private void Validate()
        {
            Debug.Assert(_maxDistanceList.Count > 0, "Max distance list is empty");
            Debug.Assert(_maxDistanceList.Skip(1).All(x => x > float.Epsilon), "Max distance element is too small");
            Debug.Assert(_maxDistanceList.SequenceEqual(_maxDistanceList.OrderBy(x => x)), $"Max distance is not ordered\n{string.Join(",\n", _maxDistanceList)}");
            Debug.Assert(_defaultRotations.Length > 0, "Default rotation array is empty");
        }

        // Todo: 船に沿わせるための処理
        // Todo: スイープ後の回転を安定させる
        // Todo: スイープが貫通する問題を修正する
        private void Update()
        {
            // FABRIKで障害物がない時のボーン位置を計算
            IKSolver solver = _ik.GetIKSolver();

            List<IKSolver.Point> temp = solver.GetPoints().DistinctBy(x => x.transform).ToList();

            IKSolver.Point[] points = temp.Where(x => temp.Count(y => y.transform == x.transform) == 1).ToArray();

            if (_debugFabrikPoints)
            {
                IKFollowerDebug.DebugDrawSpheres(points.Select(x => x.solverPosition).ToArray(), _radius * .2f, Color.yellow);
                IKFollowerDebug.DebugDrawLine(points.Select(x => x.solverPosition).ToArray(), Color.yellow);
            }

            if (_debugFabrikUpward)
            {
                foreach (var p in points)
                {
                    Debug.DrawRay(p.transform.position, p.transform.up, Color.yellow);
                }
            }

            // スイープ移動した位置を計算
            CalcSweptPosition(_followers, points, _radius, _layerMask, out Point[] sweptPoints, out bool[] rigidbodyHitFlags);

            if (_debugSweptPoints)
                IKFollowerDebug.DebugDrawLine(sweptPoints, Color.green);

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

            // 衝突のあるセクションを凸包に変換
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
                IKFollowerDebug.DebugDrawSpheres(hullPoints, _radius * .2f, Color.magenta);
                IKFollowerDebug.DebugDrawLine(hullPoints, Color.magenta);
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
                IKFollowerDebug.DebugDrawSpheres(notCollidedPoint, _radius * .9f, Color.red);

            var resultPoints = hullPoints.Concat(sweptPoints.Skip(hitCount)).ToArray();

            IPointInterpolator hullSpline = new SplinePointInterpolator(resultPoints);
            Point[] hullSolvedPoints = hullSpline.Evaluate(_maxDistanceList);

            if (_debugConcat)
                IKFollowerDebug.DebugDrawSpheres(hullSolvedPoints, _radius * .9f, Color.Lerp(Color.red, Color.yellow, 0.5f));

            UpdatePosition(hullSolvedPoints);

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

        private static void CalcSweptPosition(Rigidbody[] followerBodies, IKSolver.Point[] ikPoints, float radius, LayerMask layerMask, out Point[] sweptPoints,
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
                    .Where(x => (layerMask.value & (1 << x.transform.gameObject.layer)) != 0)
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

        private void UpdatePosition(Point[] points)
        {
            for (int i = 0; i < _followers.Length; i++)
            {
                if (_followers[i] == null) continue;

                _followers[i].MovePosition(points[i].Position);
                _followers[i].MoveRotation(points[i].Rotation);
            }
        }
    }
}
