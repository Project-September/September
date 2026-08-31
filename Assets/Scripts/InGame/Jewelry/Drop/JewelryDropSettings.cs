using System;
using InGame.Health;
using InGame.Jewelry.Common;
using September.InGame.Jewelry.Drop.Strategies;
using UnityEngine;

namespace September.InGame.Jewelry.Drop
{
    [Serializable]
    public class JewelryDropSettings
    {
        [SerializeField] private JewelryType _jewelryType;
        public JewelryType JewelryType => _jewelryType;

        [Header("端数処理")]
        [SerializeField] private RoundingMethod _higherRoundingMethod = RoundingMethod.Ceiling;
        [SerializeField] private RoundingMethod _lowerRoundingMethod = RoundingMethod.Floor;
        [Tooltip("端数処理を適用する順位閾値 (1:一位, 0:最下位)"), Range(0f, 1f)]
        [SerializeField] private float _roundingThresholdNormalizedRank = 0.5f;

        [SubclassSelector, SerializeReference] private IJewelryDropStrategy[] _dropStrategies;

        public int GetDropAmount(HitData hitData, IJewelryContainer jewelryContainer, bool outputLog = false)
        {
            DropInfo dropInfo = new();

            foreach (IJewelryDropStrategy strategy in _dropStrategies)
            {
                int amount = strategy.GetDropAmount(hitData, _jewelryType, jewelryContainer, ref dropInfo);

                if (outputLog) JewelryDropLogger.AppendStrategyLog(strategy, amount);
            }

            JewelryDropProcessUtility.RankDamagePenalty(hitData, _higherRoundingMethod, _lowerRoundingMethod, _roundingThresholdNormalizedRank, ref dropInfo);

            int jewelryCount = jewelryContainer.GetJewelryCount(_jewelryType);

            int dropAmount = Mathf.Min(Mathf.RoundToInt(dropInfo.Amount), jewelryCount);

            if (outputLog) JewelryDropLogger.JoinSettingsLog(_jewelryType, dropAmount);

            return dropAmount;
        }
    }

    public enum RoundingMethod
    {
        Floor,
        Ceiling,
        Round,
        None,
    }

    public static class RoundUtility
    {
        public static float Apply(float value, RoundingMethod roundingMethod)
        {
            return roundingMethod switch
            {
                RoundingMethod.Floor => Mathf.Floor(value),
                RoundingMethod.Ceiling => Mathf.Ceil(value),
                RoundingMethod.Round => Mathf.Round(value),
                RoundingMethod.None => value,
                _ => throw new ArgumentOutOfRangeException(nameof(roundingMethod), roundingMethod, null)
            };
        }
    }
}
