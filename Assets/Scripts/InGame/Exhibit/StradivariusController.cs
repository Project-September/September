using Fusion;
using InGame.Health;
using InGame.Player;
using September.Common;
using September.InGame.Common;
using UnityEngine;

namespace InGame.Exhibit
{
    public class StradivariusController : NetworkBehaviour
    {
        [SerializeField] private int _healAmount;
        public void HealPlayer(PlayerRef playerRef)
        {
             var playerHealth = StaticServiceLocator.Instance.Get<InGameManager>().PlayerDataDic[playerRef].GetComponent<PlayerHealth>();
             var hitdata = new HitData(HitActionType.Heal,_healAmount, playerRef,playerRef);
             playerHealth.TakeHit(ref hitdata);
        }

      
    }
}

