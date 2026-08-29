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
    public class JewelryDropHitProcessor : IHitProcessor
    {
        [SerializeField] private DropType _dropType;

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

            int dropAmount = Mathf.CeilToInt(jewelryContainer.GetJewelryCount(JewelryType.NormalGem) * (hitData.Amount / 100f));

            int count = jewelryContainer.DropJewelry(dropAmount, _resultBuffer);

            Debug.Log($"[JewelryDropHitProcessor] Dropped {count} jewelries");

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
