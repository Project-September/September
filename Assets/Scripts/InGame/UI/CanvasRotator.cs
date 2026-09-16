using UnityEngine;

namespace September.InGame
{
    /// <summary>
    /// キャンバスをカメラに向かせるコンポーネント
    /// </summary>
    public class CanvasRotator : MonoBehaviour
    {
        private Camera _camera;

        public void Awake()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            // カメラに向ける
            transform.forward = _camera.transform.forward * -1;
        }
    }
}
