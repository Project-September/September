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
        [Tooltip("霧が表示される時間"), SerializeField] private float _fogDuration = 3.0f;
        [Networked] private TickTimer _tickTimer { get; set; }
        private bool _isFogActive = false;

        /// <summary> RPCでインタラクト時に霧と嵐を表示する </summary>
        /// <param name="interactPlayerRef"></param>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_OnInteractStart(PlayerRef interactPlayerRef)
        {
            _tickTimer = TickTimer.CreateFromSeconds(Runner, _fogDuration); // 霧の表示時間を設定

            if (Runner.LocalPlayer != interactPlayerRef)
            {
                _fogController.ShowFog();
                _isFogActive = true;
            }

            _stormManager.StartStorm(); // 全員に嵐の表示
        }

        public override void FixedUpdateNetwork()
        {
            if (_tickTimer.Expired(Runner) && _fogController != null) RPC_OnFogEnd();
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_OnFogEnd()
        {
            if (_isFogActive == true)
            {
                _fogController.HideFog();
                _isFogActive = false;
            }
        }
    }
}