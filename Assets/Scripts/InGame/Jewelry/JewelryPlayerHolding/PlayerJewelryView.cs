using TMPro;
using UnityEngine;
using Fusion;
using System;
using UnityEngine.UI;
using InGame.Jewelry.Common;

namespace InGame.Jewelry
{
    public class PlayerJewelryView : NetworkBehaviour
    {
        [Serializable]
        class JewelryUI
        {
            [SerializeField] Image _jewelryImage;
            [SerializeField] TextMeshProUGUI _jewelryCountText;

            public Image JewelryImage => _jewelryImage;
            public TextMeshProUGUI JewelryCountText => _jewelryCountText;
        }

        [SerializeField] CanvasGroup _canvasGroup;
        [SerializeField] JewelryUI[] _jewelryUIArray;
        Camera _camera;

        public override void Spawned()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            // カメラに向ける
            _canvasGroup.gameObject.transform.forward = _camera.transform.forward * -1;
        }

        /// <summary>
        /// 表示の初期化メソッド（アイコンのみ）
        /// </summary>
        /// <param name="jewelryType">宝石の種類</param>
        /// <param name="sprite">アイコン</param>
        public void Init(JewelryType jewelryType, Sprite sprite)
        {
            // 宝石の所持情報を満足に表示できない場合はreturn
            if (_jewelryUIArray == null
                || _jewelryUIArray.Length <= 0
                || _jewelryUIArray.Length < (int)JewelryType.JewelryTypeCount) return;

            var image = _jewelryUIArray[(int)jewelryType].JewelryImage;
            if (image == null) return;
            image.sprite = sprite;
        }

        /// <summary>
        /// 宝石の個数表示を更新するメソッド
        /// </summary>
        /// <param name="jewelryType">宝石の種類</param>
        /// <param name="count">宝石の数</param>
        public void UpdateJewelryCount(JewelryType jewelryType, int count)
        {
            // 宝石の所持情報を満足に表示できない場合はreturn
            if (_jewelryUIArray == null
                || _jewelryUIArray.Length <= 0
                || _jewelryUIArray.Length < (int)JewelryType.JewelryTypeCount) return;

            var text = _jewelryUIArray[(int)jewelryType].JewelryCountText;
            if (text == null) return;
            text.text = "× " + count;
        }
    }
}
