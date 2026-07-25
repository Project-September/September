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

        [SerializeField] JewelryUI[] _jewelryUIArray;
        [SerializeField] private bool _hideLocal = true;

        public override void Spawned()
        {
            if (_hideLocal && Object.InputAuthority == Runner.LocalPlayer)
            {
                gameObject.SetActive(false);
                // TODO : ローカルの時は左上に表示する
            }
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
                || _jewelryUIArray.Length <= 0)
            {
                Debug.LogWarning("宝石UIが不足しています");
                return;
            }

            if (_jewelryUIArray.Length <= (int)jewelryType) return;
            var image = _jewelryUIArray[(int)jewelryType].JewelryImage;
            if (image == null)
            {
                Debug.LogWarning("Imageコンポーネントが見つかりませんでした");
                return;
            }
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
                || _jewelryUIArray.Length <= 0)
            {
                Debug.LogWarning("宝石UIが不足しています");
                return;
            }

            if (_jewelryUIArray.Length <= (int)jewelryType) return;
            var text = _jewelryUIArray[(int)jewelryType].JewelryCountText;
            if (text == null)
            {
                Debug.LogWarning("テキストコンポーネントが見つかりませんでした");
                return;
            }
            text.text = "× " + count;
        }
    }
}
