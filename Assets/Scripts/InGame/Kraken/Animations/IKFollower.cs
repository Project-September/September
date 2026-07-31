using System;
using System.Collections.Generic;
using System.Linq;
using RootMotion.FinalIK;
using September.InGame.Kraken.Animations;
using UnityEngine;
using UnityEngine.Profiling;

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

            public static Point Slerp(Point p0, Point p1, float t)
            {
                return new Point(
                    Vector3.Slerp(p0.Position, p1.Position, t),
                    Quaternion.Slerp(p0.Rotation, p1.Rotation, t)
                    );
            }
        }

        [SerializeField] private IK _ik;
        [SerializeField] private Transform[] _followers;
        [SerializeField] private float _radius = .55f;
        [SerializeField] private TentacleConstraintSolver _constraintSolver;
        [SerializeField] private int _subPointCount = 3;

        [SerializeField, HideInInspector] private List<float> _maxDistanceList = new();
        [SerializeField, HideInInspector] private Quaternion[] _defaultRotations;

        [Header("Debug Settings")]
        [SerializeField] private bool _debugFabrikPoints;
        [SerializeField] private bool _debugFabrikAxis;
        [SerializeField] private bool _debugSolvedPoints;
        [SerializeField] private bool _debugSolvedAxis;
        [SerializeField] private bool _debugSolvedSpline;
        [SerializeField] private bool _debugSolvedResamplingPoints;
        [SerializeField] private bool _debugSolvedResamplingAxis;
        [SerializeField] private bool _debugSolvedResamplingSpline;

        private IKSolverPointProvider _pointProvider;

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

            _pointProvider = new IKSolverPointProvider(_ik.GetIKSolver());

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
        private void Update()
        {
            Profiler.BeginSample("GetPoints");
            // FABRIKで障害物がない時のボーン位置を計算
            Point[] points = _pointProvider.GetPoints();
            Profiler.EndSample();

            IKFollowerDebug.DebugDraw(points.ToArray(), _radius * .2f, Color.yellow, _debugFabrikPoints, _debugFabrikPoints, _debugFabrikAxis);

            Profiler.BeginSample("EvaluateSpline SubPoints");
            Span<float> maxDistanceList = stackalloc float[_maxDistanceList.Count];
            for (int i = 0; i < _maxDistanceList.Count; i++)
            {
                maxDistanceList[i] = _maxDistanceList[i];
            }
            SlerpInterpolator spline = new(points, maxDistanceList);
            Span<Point> subdividedPoints = stackalloc Point[points.Length * _subPointCount];
            spline.Evaluate(points.Length * _subPointCount, ref subdividedPoints);
            Profiler.EndSample();

            Profiler.BeginSample("SolveCollision");
            _constraintSolver.Solve(ref subdividedPoints, Time.deltaTime);
            Span<Point> solvedPoints = subdividedPoints;
            Profiler.EndSample();

            IKFollowerDebug.DebugDraw(solvedPoints.ToArray(), 6f, Color.red, _debugSolvedPoints, _debugSolvedPoints, _debugSolvedAxis);

            Profiler.BeginSample("Resampling");
            Span<float> solvedDistances = stackalloc float[solvedPoints.Length];
            for (int i = 1; i < solvedDistances.Length; i++)
            {
                solvedDistances[i] =
                    solvedDistances[i - 1] +
                    (solvedPoints[i].Position - solvedPoints[i - 1].Position).magnitude;
            }
            SlerpInterpolator solvedSpline = new(solvedPoints, solvedDistances);
            Span<Point> solvedResamplingPoints = stackalloc Point[_maxDistanceList.Count];
            solvedSpline.Evaluate(_maxDistanceList, ref solvedResamplingPoints);
            Profiler.EndSample();

            IKFollowerDebug.DebugDraw(solvedResamplingPoints.ToArray(), 6f, Color.magenta, _debugSolvedResamplingPoints, _debugSolvedResamplingPoints, _debugSolvedResamplingAxis);

            Profiler.BeginSample("UpdatePoints");
            UpdatePosition(solvedResamplingPoints);
            Profiler.EndSample();
        }

        private void UpdatePosition(Span<Point> points)
        {
            for (int i = 0; i < _followers.Length; i++)
            {
                if (_followers[i] == null) continue;

                _followers[i].transform.position = points[i].Position;
                _followers[i].transform.rotation = points[i].Rotation;
            }
        }
    }
}
