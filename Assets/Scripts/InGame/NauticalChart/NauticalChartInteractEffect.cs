using Fusion;
using InGame.Interact;
using NaughtyAttributes;
using UnityEngine;

namespace InGame.Exhibit
{
    /// <summary> 海図のインタラクション効果を制御するクラス </summary>
    public class NauticalChartInteractEffect : NetworkBehaviour
    {
        [SerializeReference, SubclassSelector] private IFogController _fogController;

        public void OnInterractStart(IInteractableContext context, InteractableBase target)
        {
            var playerRef = PlayerRef.FromEncoded(context.Interactor);
            RPC_IsRestrictedPlayer(playerRef);
        }

        /// <summary> RPCで制限されたプレイヤーかどうかを判定する </summary>
        /// <param name="interactPlayerRef"></param>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_IsRestrictedPlayer(PlayerRef interactPlayerRef)
        {
            if (Runner.LocalPlayer == interactPlayerRef) return;
            _fogController.ShowFog();
        }
    }
}
