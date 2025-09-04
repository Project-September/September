using CRISound;
using Fusion;
using InGame.Health;
using InGame.Player;
using September.Common;
using September.InGame.Common;
using September.InGame.Effect;
using UnityEngine;

namespace InGame.Exhibit
{
    public class HealInstrumentController : NetworkBehaviour
    {
        [SerializeField] private int _healAmount;
        private EffectSpawner _effectSpawner;
        [SerializeField] private Vector3 _offset;
        public void HealPlayer(PlayerRef playerRef)
        {
             var playerObject = StaticServiceLocator.Instance.Get<InGameManager>().PlayerDataDic[playerRef];
             var playerHealth = playerObject.GetComponent<PlayerHealth>();
             var hitdata = new HitData(HitActionType.Heal,_healAmount, playerRef,playerRef);
             playerHealth.TakeHit(ref hitdata);
             PlayEffect(playerObject);
        }
        
        private void PlayEffect(NetworkObject netObj)
        {
            // Effect生成処理
            _effectSpawner ??= StaticServiceLocator.Instance.Get<EffectSpawner>();
            _effectSpawner?.RequestPlayOneShotEffect(EffectType.Heal, netObj.transform.position + _offset,
                netObj.transform.rotation,netObj.transform);
        }

      
    }
}

