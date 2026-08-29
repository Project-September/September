using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using InGame.Jewelry.Common;
using September.InGame.Jewelry;
using UnityEngine;

namespace September.InGame.Health
{
    public class JewelryDropHitProcessor : IHitProcessor
    {
        private readonly IJewelry[] _resultBuffer = new IJewelry[30];

        public DropType DropType { get; set; }

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
            for (int i = 0; i < count; i++)
            {
                if (_resultBuffer[i] is global::InGame.Jewelry.Jewelry jewel)
                {
                    jewel.PickupFrom(hitData.ExecutorRef).Forget();
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
