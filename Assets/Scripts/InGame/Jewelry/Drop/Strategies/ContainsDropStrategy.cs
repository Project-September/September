using System;
using InGame.Health;
using InGame.Jewelry.Common;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace September.InGame.Jewelry.Drop.Strategies
{
    [Serializable]
    public class ContainsDropStrategy : IJewelryDropStrategy
    {
        [Header("所持宝石スコアに応じたドロップ確率")]
        [SerializeField, Label("最低保証個数"), AllowNesting] private int _containerMinDropCount;
        [SerializeField, CurveRange(0f, 0f, 20f, 1f, EColor.Red)]
        private AnimationCurve _containerDropRate = AnimationCurve.Linear(0, 0, 11, 1);

        [Header("ダメージ量に応じたドロップ量の変動")]
        [SerializeField] private AnimationCurve _damageMultiplierCurve = AnimationCurve.Linear(0, 1, 200, 4);

        [Header("ダメージ量に応じたドロップ確率の変動")]
        [SerializeField] private AnimationCurve _damageChanceCurve = AnimationCurve.Linear(0, 0, 200, 0);

        public int GetDropAmount(HitData hitData, JewelryType jewelryType, IJewelryContainer jewelryContainer)
        {
            int jewelryCount = jewelryContainer.CalculateJewelryScore();
            float damageMultiplier = _damageMultiplierCurve.Evaluate(hitData.Amount);
            float chance = _containerDropRate.Evaluate(jewelryCount) + _damageChanceCurve.Evaluate(hitData.Amount);
            int dropAmount = Mathf.RoundToInt(
                    (Random.value <= chance ? 1 : 0) * damageMultiplier
                );
            return Mathf.Max(dropAmount, _containerMinDropCount);
        }
    }
}
