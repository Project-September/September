using UnityEngine;

namespace InGame.Exhibit
{
    /// <summary>
    /// 放物線をLineRendererで描画するモノビヘイビア
    /// </summary>
    public class TrajectoryVisualizer : MonoBehaviour
    {
        [Header("描画設定")]
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private int _segmentCount = 30;
        [SerializeField] private float _timeStep = 0.1f;

        private void Awake()
        {
            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
            }
        }

        /// <summary>
        /// 放物線を表示する
        /// </summary>
        public void ShowTrajectory(Vector3 startPos, Vector3 initialVelocity)
        {
            Vector3[] points = new Vector3[_segmentCount];
            Vector3 currentPosition = startPos;
            Vector3 currentVelocity = initialVelocity;

            for (int i = 0; i < _segmentCount; i++)
            {
                points[i] = currentPosition;
                currentVelocity += Physics.gravity * _timeStep;
                Vector3 nextPosition = currentPosition + currentVelocity * _timeStep;

                if (Physics.Linecast(currentPosition, nextPosition, out var hit))
                {
                    points[i + 1 >= _segmentCount ? i : i + 1] = hit.point;
                    _lineRenderer.positionCount = i + 2;
                    break;
                }

                currentPosition = nextPosition;
            }

            _lineRenderer.enabled = true;
            _lineRenderer.SetPositions(points);
        }

        /// <summary>
        /// 放物線描画を非表示にする
        /// </summary>
        public void Hide()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }
    }
}