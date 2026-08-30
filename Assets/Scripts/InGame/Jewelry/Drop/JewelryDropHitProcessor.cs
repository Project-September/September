using System;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using InGame.Jewelry.Common;
using September.InGame.Jewelry;
using UnityEngine;

namespace September.InGame.Health
{
    [Serializable]
    public class JewelryDropSettings
    {
        [SerializeField] private JewelryType _jewelryType;
        public JewelryType JewelryType => _jewelryType;

        [SubclassSelector, SerializeReference] private IJewelryDropStrategy[] _dropStrategies;

        public int GetDropAmount(HitData hitData, IJewelryContainer jewelryContainer)
        {
            int dropAmount = 0;

            foreach (IJewelryDropStrategy strategy in _dropStrategies)
            {
                int amount = strategy.GetDropAmount(hitData, _jewelryType, jewelryContainer);
                Debug.Log($"{strategy.GetType().Name} amount:{amount}");
                dropAmount += amount;
            }

            int jewelryCount = jewelryContainer.GetJewelryCount(_jewelryType);

            return Mathf.Min(dropAmount, jewelryCount);
        }
    }

    [Serializable]
    public class JewelryDropHitProcessor : IHitProcessor
    {
        [SerializeField] private DropType _dropType;

        [Header("ドロップ数設定")]
        [SerializeField] private JewelryDropSettings[] _dropSettingsList;

        private readonly IJewelry[] _resultBuffer = new IJewelry[30];

        public DropType DropType { get => _dropType; set => _dropType = value; }

        public JewelryDropHitProcessor(DropType dropType)
        {
            DropType = dropType;
        }

        public void OnHitTaken(HitData hitData)
        {
            var runner = NetworkRunner.Instances[0];
            if (!runner) return;

            var targetObj = runner.GetPlayerObject(hitData.TargetRef);
            var jewelryContainer = targetObj.GetComponentInChildren<IJewelryContainer>();
            if (jewelryContainer == null) return;

            foreach (var dropSettings in _dropSettingsList)
            {
                DropJewelry(dropSettings, jewelryContainer, hitData);
            }
        }

        private void DropJewelry(JewelryDropSettings dropSettings, IJewelryContainer victimJewelryContainer, HitData hitData)
        {
            int dropAmount = dropSettings.GetDropAmount(hitData, victimJewelryContainer);

            int count = victimJewelryContainer.DropJewelry(dropSettings.JewelryType, dropAmount, _resultBuffer);

            // Dropの場合、デフォルトでその場にドロップするので何もしない
            if (DropType == DropType.Drop) return;

            // Pickup処理
            PickupJewelries(_resultBuffer.AsSpan(..count), hitData.ExecutorRef);
        }

        public static void PickupJewelries(ReadOnlySpan<IJewelry> jewelries, PlayerRef pickupPlayerRef)
        {
            foreach (IJewelry jewelry in jewelries)
            {
                if (jewelry is global::InGame.Jewelry.Jewelry jewel)
                {
                    jewel.PickupFrom(pickupPlayerRef).Forget();
                }
            }
        }
    }

    public enum DropType
    {
        Drop,
        Pickup
    }
}
