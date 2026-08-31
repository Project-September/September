using System;
using InGame.Health;
using InGame.Jewelry.Common;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace September.InGame.Jewelry.Drop.Strategies
{
    [Serializable]
    public class ChanceDropStrategy : IJewelryDropStrategy
    {
        [Header("ダメージに応じた確率ドロップ")]
        [SerializeField] private int _damageChancedDropRate = 20;
        [SerializeField, CurveRange(0f, 0f, 200f, 1f, EColor.Red)]
        private AnimationCurve _dropChanceDamageCurve = AnimationCurve.Linear(0, 0, 200, 1);
        [SerializeField, CurveRange(0f, 0f, 1f, 10f, EColor.Blue)]
        private AnimationCurve _dropChanceMinCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public int GetDropAmount(HitData hitData, JewelryType jewelryType, IJewelryContainer jewelryContainer)
        {
            float t = _dropChanceDamageCurve.Evaluate(hitData.Amount);
            int minDrop = Mathf.FloorToInt(_dropChanceMinCurve.Evaluate(t));
            int dropAmount = Mathf.RoundToInt(Random.Range(minDrop, _damageChancedDropRate));
            return dropAmount;
        }
    }
}
