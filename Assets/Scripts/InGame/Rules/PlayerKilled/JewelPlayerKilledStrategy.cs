using System;
using Fusion;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace September.InGame.Rules
{
    [Serializable]
    public class JewelPlayerKilledStrategy : IPlayerKilledStrategy
    {
        [SerializeField, Tooltip("死亡時の所持宝石数からドロップする数")] private int _minDropAmount = 1;
        [SerializeField, Tooltip("死亡時の所持宝石数からドロップする割合")] private float _minDropRatio = 0f;
        [SerializeField, Tooltip("minDropXXX の計算後に残った所持宝石数からドロップする割合")] private float _additionalDropRatio = 0.5f;

        public void ProcessKillEvent(PlayerRef killer, PlayerRef victim)
        {
            NetworkObject victimObj = PlayerDatabase.Instance.PlayerObjectDic.Get(victim);

            var container = victimObj.GetComponent<IJewelryContainer>();

            int minDrop = _minDropAmount + Mathf.FloorToInt(container.JewelryCount * _minDropRatio);
            int sumDrop = minDrop + Mathf.FloorToInt((container.JewelryCount - minDrop) * _additionalDropRatio);
            int drop = Mathf.Min(sumDrop, container.JewelryCount);

            container.DropJewelry(drop);
        }
    }
}
