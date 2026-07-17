using TMPro;
using UnityEngine;
using Fusion;

namespace InGame
{
    public class JewelryView : NetworkBehaviour
    {
        [SerializeField] CanvasGroup _canvasGroup;
        [SerializeField] TextMeshProUGUI _jewelryCountText;
        Camera _camera;

        public override void Spawned()
        {
            _camera = Camera.main;
        }

        public override void FixedUpdateNetwork()
        {
            // カメラに向ける
            _canvasGroup.gameObject.transform.forward = _camera.transform.forward * -1;
        }

        /// <summary>
        /// 宝石の個数表示を更新するメソッド
        /// </summary>
        /// <param name="count">宝石の数</param>
        public void UpdateJewelryCount(int count)
        {
            _jewelryCountText.text = "× " + count.ToString();
        }
    }
}
