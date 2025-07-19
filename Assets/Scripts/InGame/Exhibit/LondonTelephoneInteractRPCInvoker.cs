using Fusion;
using NaughtyAttributes;
using September.Common;
using September.InGame.Effect;
using UnityEngine;

namespace InGame.Exhibit
{
    public class LondonTelephoneInteractRPCInvoker : NetworkBehaviour
    {
        [SerializeField,Label("敵下に表示するEffect")] private ParticleSystem _rippleSpawnPositionsEffect;
        
        private EffectSpawner _effectSpawner;
        
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcRequestInteraction(PlayerRef requestingPlayer)
        {
            // ここで他のプレイヤーに通知
            foreach (var player in Runner.ActivePlayers)
            {
                Debug.Log($"{player} is interacted with {requestingPlayer}");
                
                if (player != requestingPlayer)
                {
                    ShowEffect(player);
                }
            }
        }

        private void ShowEffect(PlayerRef player)
        {
            Runner.TryGetPlayerObject(player,out var playerObject);
            // 実行されたPlayerの地面にEffectを任意の数再生する
            Vector3 effectPosition = playerObject.transform.position + Vector3.down * 0.5f;
            
            // Effect生成処理
            _effectSpawner ??= StaticServiceLocator.Instance.Get<EffectSpawner>();
            _effectSpawner?.RequestPlayOneShotEffect(EffectType.LondonTelephone, effectPosition,
                new());
        }
    }
}