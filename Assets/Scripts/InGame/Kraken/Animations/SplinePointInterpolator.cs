using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace September.InGame.Kraken
{
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