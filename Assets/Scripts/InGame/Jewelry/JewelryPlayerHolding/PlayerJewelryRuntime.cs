using Fusion;
using InGame.Jewelry.Common;
using System;
using UnityEngine;

namespace InGame.Jewelry
{
    /// <summary>
    /// 宝石の所持情報を管理するクラス
    /// 値を同期させるためにNetworkBehaviourを継承させている
    /// </summary>
    public class PlayerJewelryRuntime : NetworkBehaviour
    {
        /// <summary>宝石の種類と個数の対応表</summary>
        [Networked, Capacity((int)JewelryType.Count), HideInInspector]
        public NetworkArray<int> JewelryCounts => default;

        event Action<JewelryType, Sprite> _onInitialize;
        event Action<JewelryType, int> _onUpdateJewelryQuantity;
        public Action OnInitialize(Action<JewelryType, Sprite> act)
        {
            _onInitialize += act;
            return () => _onInitialize -= act;
        }
        public Action OnUpdateJewelryQuantity(Action<JewelryType, int> act)
        {
            _onUpdateJewelryQuantity += act;
            return () => _onUpdateJewelryQuantity -= act;
        }

        public void Init(PlayerJewelryModel model)
        {
            if (model == null || model.HoldingJewelryInfos == null)
            {
                throw new InvalidOperationException();
            }

            // 対応表の作成
            foreach (var info in model.HoldingJewelryInfos)
            {
                var jewelryType = info.JewelryType;
                var quantity = info.JewelryCount;
                JewelryCounts.Set((int)jewelryType, quantity);
                _onInitialize?.Invoke(jewelryType, info.JewelrySprite);
                _onUpdateJewelryQuantity?.Invoke(jewelryType, quantity);
            }
        }

        /// <summary>
        /// 宝石を獲得した時に呼ばれるメソッド
        /// </summary>
        /// <param name="jewelryType">獲得した宝石の種類</param>
        public void GetJewelry(JewelryType jewelryType)
        {
            // enumの最後の要素（Count）が渡された場合はreturn
            if (jewelryType == JewelryType.Count) return;

            var currentQuantity = JewelryCounts.Get((int)jewelryType);
            JewelryCounts.Set((int)jewelryType, currentQuantity + 1);
            _onUpdateJewelryQuantity?.Invoke(jewelryType, JewelryCounts.Get((int)jewelryType));
        }

        /// <summary>
        /// 宝石を落とした時に呼ばれるメソッド
        /// </summary>
        /// <param name="jewelryType">落とした宝石の種類</param>
        public void DropJewelry(JewelryType jewelryType)
        {
            // enumの最後の要素（Count）が渡された場合はreturn
            if (jewelryType == JewelryType.Count) return;

            var currentQuantity = JewelryCounts.Get((int)jewelryType);
            JewelryCounts.Set((int)jewelryType, currentQuantity - 1);
            _onUpdateJewelryQuantity?.Invoke(jewelryType, JewelryCounts.Get((int)jewelryType));
        }
    }
}
