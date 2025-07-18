using Fusion;
using NaughtyAttributes;
using UnityEngine;

namespace InGame.Exhibit
{
    public class LondonTelephoneInteractRPCInvoker : NetworkBehaviour
    {
        [SerializeField,Label("敵下に表示するEffect")] private ParticleSystem _rippleSpawnPositionsEffect;
        
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RpcRequestInteraction(PlayerRef requestingPlayer)
        {
            Debug.Log($"Interaction requested by {requestingPlayer}");

            // ここで他のプレイヤーに通知
            foreach (var player in Runner.ActivePlayers)
            {
                if (player != requestingPlayer)
                {
                    RpcShowEffect(player);
                }
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcShowEffect([RpcTarget] PlayerRef target)
        {
            if (Runner.LocalPlayer == target)
            {
                ShowEffect();
            }
        }

        private void ShowEffect()
        {
            // 任意の位置にエフェクトを表示（オブジェクトの下）
            Vector3 effectPosition = transform.position + Vector3.down * 0.5f;
            // ここにエフェクト生成処理を書く（例えばInstantiateなど）
            Debug.Log("Showing effect at " + effectPosition);
            // 例: Instantiate(effectPrefab, effectPosition, Quaternion.identity);
        }
    }
}