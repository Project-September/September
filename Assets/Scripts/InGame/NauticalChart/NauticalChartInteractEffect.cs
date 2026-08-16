using Fusion;
using September;
using UnityEngine;

namespace InGame.Exhibit
{
    /// <summary> 海図のインタラクション効果を制御するクラス </summary>
    public class NauticalChartInteractEffect : NetworkBehaviour
    {
        [SerializeReference, SubclassSelector] private IFogController _fogController;
        [SerializeField] private StormManager _stormManager;

        /// <summary> RPCでインタラクト時に霧と嵐を表示する </summary>
        /// <param name="interactPlayerRef"></param>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_OnInteractStart(PlayerRef interactPlayerRef)
        {
            if (Runner.LocalPlayer != interactPlayerRef) _fogController.ShowFog(); // 自分以外のプレイヤーに霧を表示
            _stormManager.StartStorm(); // 全員に嵐の表示
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            _fogController.HideFog();
        }
    }
}