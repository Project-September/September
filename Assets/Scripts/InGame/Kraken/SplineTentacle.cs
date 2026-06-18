using System;
using UnityEngine;
using UnityEngine.Splines;

namespace September.InGame.Kraken
{
    public class SplineTentacle : MonoBehaviour
    {
        [SerializeField] private SplineContainer _splineContainer;
        [SerializeField] private float _segmentLength = 0.2f;
        [SerializeField] private Transform[] _points;
        [SerializeField] private AnimationCurve _timeCurve;

        private float _elapsedTime;

        private void Update()
        {
            if (!_splineContainer || _splineContainer.Spline == null) return;

            _elapsedTime += Time.deltaTime;

            float t = _timeCurve.Evaluate(_elapsedTime);

            for (int i = 0; i < _points.Length; i++)
            {
                var target = _points[i];
                var segmentPoint = GetPoint(i, t);

                if (target)
                {
                    target.position = segmentPoint;
                }
                else
                {
                    Gizmos.DrawWireSphere(segmentPoint, .5f);
                }
            }

            for (int i = 0; i < _points.Length - 1; i++)
            {
                var p0 = _points[i];
                var p1 = _points[i + 1];
                Debug.Log(Vector3.Distance(p0.position, p1.position));
            }
        }

        private void OnDrawGizmosSelected()
        {
            Update();

            for (int i = 0; i < _points.Length; i++)
            {
                var target = _points[i];
                var segmentPoint = GetPoint(i, _timeCurve.Evaluate(_elapsedTime));

                if (!target)
                {
                    Gizmos.DrawWireSphere(segmentPoint, .5f);
                }
            }
        }

        private Vector3 GetPoint(int index, float t)
        {
            var splines = _splineContainer.Splines;
            int splineIndex = Mathf.Clamp(Mathf.FloorToInt(t), 0, splines.Count - 2);

            if (splines.Count < 2) return Vector3.zero;

            var from = splines[splineIndex];
            var to = splines[splineIndex + 1];

            if (Mathf.FloorToInt(t) <= splines.Count - 2)
            {
                t %= 1;
            }

            return EaseSpline(t, from, to, index);
        }

        private Vector3 EaseSpline(float t, Spline from, Spline to, int index)
        {
            return Vector3.Slerp(GetPointSpline(from, index), GetPointSpline(to, index), t);
        }

        private Vector3 GetPointSpline(Spline spline, int index)
        {
            if (spline == null) throw new ArgumentNullException();

            float t = _segmentLength * index / spline.GetLength();
            return spline.EvaluatePosition(t);
        }
    }
}
