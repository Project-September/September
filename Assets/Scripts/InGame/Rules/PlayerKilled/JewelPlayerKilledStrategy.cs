using System;
using Fusion;
using InGame.Health;
using InGame.Jewelry;
using InGame.Jewelry.Common;
using September.Common;
using September.InGame.Health;
using UnityEngine;

namespace September.InGame.Rules
{
    [Serializable]
    public class JewelPlayerKilledStrategy : IPlayerKilledStrategy
    {
        [SerializeField, Tooltip("死亡時の所持宝石数からドロップする数")] private int _minDropAmount = 1;
        [SerializeField, Tooltip("死亡時の所持宝石数からドロップする割合")] private float _minDropRatio = 0f;
        [SerializeField, Tooltip("minDropXXX の計算後に残った所持宝石数からドロップする割合")] private float _additionalDropRatio = 0.5f;

        private readonly IJewelry[] _dropResultBuffer = new IJewelry[30];

        public void ProcessKillEvent(HitData hitData)
        {
            var killer = hitData.ExecutorRef;
            var victim = hitData.TargetRef;

            NetworkObject victimObj = PlayerDatabase.Instance.PlayerObjectDic.Get(victim);

            // runtimeから現在の宝石所持情報を取得
            var runtime = victimObj.GetComponentInChildren<PlayerJewelryRuntime>();

            // 仮で通常宝石で計算
            var jewelryQuantity = runtime.GetJewelryQuantity(JewelryType.NormalGem);
            int minDrop = _minDropAmount + Mathf.FloorToInt(jewelryQuantity * _minDropRatio);
            int sumDrop = minDrop + Mathf.FloorToInt((jewelryQuantity - minDrop) * _additionalDropRatio);
            int drop = Mathf.Min(sumDrop, jewelryQuantity);

            var container = victimObj.GetComponent<IJewelryContainer>();
            int count = container.DropJewelry(JewelryType.NormalGem, drop, _dropResultBuffer);

            if (hitData.HitActionType == HitActionType.RangedDamage)
            {
                JewelryDropHitProcessor.PickupJewelries(_dropResultBuffer.AsSpan(..count), killer);
            }
        }
    }
}
