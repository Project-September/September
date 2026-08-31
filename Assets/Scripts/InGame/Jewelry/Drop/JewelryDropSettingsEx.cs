using System;
using InGame.Health;
using InGame.Jewelry.Common;
using September.InGame.Jewelry.Drop.Strategies;
using September.InGame.Jewelry.Drop.Strategies.Amounts;
using September.InGame.Jewelry.Drop.Strategies.Chances;
using UnityEngine;
using Random = UnityEngine.Random;

namespace September.InGame.Jewelry.Drop
{
    [Serializable]
    public class JewelryDropSettingsEx
    {
        [SerializeField] private JewelryType _jewelryType;
        public JewelryType JewelryType => _jewelryType;

        [Header("端数処理")]
        [SerializeField] private RoundingMethod _higherRoundingMethod = RoundingMethod.Ceiling;
        [SerializeField] private RoundingMethod _lowerRoundingMethod = RoundingMethod.Floor;
        [Tooltip("端数処理を適用する順位閾値 (1:一位, 0:最下位)"), Range(0f, 1f)]
        [SerializeField] private float _roundingThresholdNormalizedRank = 0.5f;

        [Header("ドロップ確率（各要素の総和を確率として扱う）")]
        [SubclassSelector, SerializeReference] private IJewelryDropChance[] _dropChances;

        [Header("ドロップ量")]
        [SubclassSelector, SerializeReference] private IJewelryDropAmount[] _dropAmounts;

        public int GetDropAmount(HitData hitData, IJewelryContainer jewelryContainer, bool outputLog = false)
        {
            JewelryDropContext context = new(hitData, _jewelryType, jewelryContainer);

            float dropChanceSum = 0;
            foreach (IJewelryDropChance chance in _dropChances)
            {
                dropChanceSum += chance.GetChance(context);
            }
            if (dropChanceSum >= Random.value) return 0;

            foreach (IJewelryDropAmount amount in _dropAmounts)
            {
                amount.GetDropAmount(ref context);

                // if (outputLog) JewelryDropLogger.AppendStrategyLog(strategy, amount);
            }

            // JewelryDropProcessUtility.RankDamagePenalty(hitData, _higherRoundingMethod, _lowerRoundingMethod, _roundingThresholdNormalizedRank, ref dropInfo);

            int jewelryCount = jewelryContainer.GetJewelryCount(_jewelryType);

            int dropAmount = Mathf.Min(Mathf.RoundToInt(context.Amount), jewelryCount);

            // if (outputLog) JewelryDropLogger.JoinSettingsLog(_jewelryType, dropAmount);

            return dropAmount;
        }
    }
}
