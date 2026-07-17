using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace September.InGame.Kraken
{
    public class SplinePointInterpolator : IPointInterpolator
    {
        private readonly Spline _spline;
        private const float Tension = 1f;

        public SplinePointInterpolator(IKFollower.Point[] points)
        {
            _spline = CreateSpline(points, Tension);
        }

        private static Spline CreateSpline(IReadOnlyList<IKFollower.Point> points, float tension)
        {
            var spline = new Spline();
            foreach (var point in points)
            {
                // tangentの向きはクラーケン用に合わせている
                spline.Add(new BezierKnot(point.Position, Vector3.right * tension, -Vector3.right * tension, point.Rotation));
            }
            return spline;
        }

        public IKFollower.Point[] Evaluate(IReadOnlyList<float> distances)
        {
            return distances.Select(Evaluate).ToArray();
        }

        public IKFollower.Point[] Evaluate(int pointCount)
        {
            var result = new IKFollower.Point[pointCount];

            float length = _spline.GetLength();
            for (int i = 0; i < pointCount; i++)
            {
                result[i] = Evaluate(i * (length / pointCount));
            }

            return result;
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
                Debug.DrawLine(position, endPosition, Color.yellow);
                Debug.DrawRay(position, normal, Color.cyan);
                Debug.DrawRay(position, tangent, Color.magenta * new Color(1f, 1f, 1f, .3f));
            }

            IKFollowerDebug.DebugDrawAxis(_spline.Knots.Select(x => new IKFollower.Point(x.Position, x.Rotation)).ToArray());
        }
    }
}
