using InGame.Interact;
using Fusion;

namespace InGame.Exhibit
{
    /// <summary> 海図のインタラクション効果を制御するクラス </summary>
    public abstract class NauticalChartInteractEffect : NetworkBehaviour
    {

        public void OnInterractStart(IInteractableContext context, InteractableBase target)
        {
            ShowFog();
        }

        public void ShowFog()
        {
            RPC_ShowFog(Runner.LocalPlayer);
        }

        /// <summary> RPCで霧の効果を表示する </summary>
        /// <param name="interactPlayerRef"></param>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_ShowFog(PlayerRef interactPlayerRef)
        {
            SkyBoxChange();
            if (Runner.LocalPlayer == interactPlayerRef) return;
            PlayFogEffect();
        }

        protected abstract void PlayFogEffect();
        protected abstract void SkyBoxChange();
    }
}
