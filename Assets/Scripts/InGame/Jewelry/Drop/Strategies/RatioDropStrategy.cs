using System;
using InGame.Health;
using InGame.Jewelry.Common;
using September.InGame.Jewelry.Drop.Strategies;
using UnityEngine;

namespace September.InGame.Rules
{
    [Serializable]
    public class RatioDropStrategy : IJewelryDropStrategy
    {
        [SerializeField, Tooltip("死亡時の所持宝石数からドロップする数")] private int _minDropAmount = 1;
        [SerializeField, Tooltip("死亡時の所持宝石数からドロップする割合")] private float _minDropRatio = 0f;
        [SerializeField, Tooltip("minDropXXX の計算後に残った所持宝石数からドロップする割合")] private float _additionalDropRatio = 0.5f;

        public int GetDropAmount(HitData hitData, JewelryType jewelryType, IJewelryContainer jewelryContainer, ref DropInfo info)
        {
            var jewelryQuantity = jewelryContainer.GetJewelryCount(jewelryType);
            int minDrop = _minDropAmount + Mathf.FloorToInt(jewelryQuantity * _minDropRatio);
            int sumDrop = minDrop + Mathf.FloorToInt((jewelryQuantity - minDrop) * _additionalDropRatio);
            int dropAmount = Mathf.Min(sumDrop, jewelryQuantity);

            info.Amount += dropAmount;
            return dropAmount;
        }
    }
}
