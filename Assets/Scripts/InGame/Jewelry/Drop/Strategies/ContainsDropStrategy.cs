using System;
using InGame.Health;
using InGame.Jewelry.Common;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace September.InGame.Health
{
    [Serializable]
    public class ContainsDropStrategy : IJewelryDropStrategy
    {
        [Header("所持数に応じた確率ドロップ")]
        [SerializeField, Label("最低保証個数"), AllowNesting] private int _containerMinDropCount;
        [SerializeField, CurveRange(0f, 0f, 20f, 1f, EColor.Red)]
        private AnimationCurve _containerDropRate = AnimationCurve.Linear(0, 0, 11, 1);

        public int GetDropAmount(HitData hitData, JewelryType jewelryType, IJewelryContainer jewelryContainer)
        {
            int jewelryCount = jewelryContainer.CalculateJewelryScore();
            int dropAmount = Random.value <= _containerDropRate.Evaluate(jewelryCount) ? 1 : 0;
            return Mathf.Max(dropAmount, _containerMinDropCount);
        }
    }
}