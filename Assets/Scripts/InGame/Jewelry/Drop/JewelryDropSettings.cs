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

        [SubclassSelector, SerializeReference] private IJewelryDropStrategy[] _dropStrategies;

        public int GetDropAmount(HitData hitData, IJewelryContainer jewelryContainer, bool outputLog = false)
        {
            int dropAmount = 0;

            foreach (IJewelryDropStrategy strategy in _dropStrategies)
            {
                int amount = strategy.GetDropAmount(hitData, _jewelryType, jewelryContainer);
                dropAmount += amount;

                if (outputLog) JewelryDropLogger.AppendStrategyLog(strategy, amount);
            }

            int jewelryCount = jewelryContainer.GetJewelryCount(_jewelryType);

            dropAmount = Mathf.Min(dropAmount, jewelryCount);

            if (outputLog) JewelryDropLogger.JoinSettingsLog(_jewelryType, dropAmount);

            return dropAmount;
        }
    }
}
