using Fusion;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace September.InGame.Rules
{
    public class JewelPlayerKilledStrategy : IPlayerKilledStrategy
    {
        public void ProcessKillEvent(PlayerRef killer, PlayerRef victim)
        {
            NetworkObject victimObj = PlayerDatabase.Instance.PlayerObjectDic.Get(victim);

            var container = victimObj.GetComponent<IJewelryContainer>();
            container.DropJewelry(Mathf.FloorToInt(container.JewelryCount * .5f));
        }
    }
}
