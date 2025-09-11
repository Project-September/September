using Fusion;
using UnityEngine;
using CRISound;
using September.Common;

namespace September.InGame
{
    public class PlayerAudioController : NetworkBehaviour
    {
        [SerializeField] private string _sheetName = "ALLCue";
        [SerializeField] private string _footstepCueName = SoundCues.SE.OKB_Footstep.Name; // キャラによって変わる
        [SerializeField] private string _punchSwingCueName = SoundCues.SE.Player_Punch_Swing.Name;
        [SerializeField] private string _punchHitCueName = SoundCues.SE.Player_Punch_Hit.Name;
        private CuePlayAtomExPlayer.SEPlayerWith3D.Sound3D _soundPlayer;
        private CRIListenerManager _listenerManager;

        public override void Spawned()
        {
            // 3Dリスナーの設定(カメラ追従)
            if (!Object.HasInputAuthority) return;

            _listenerManager = FindFirstObjectByType<CRIListenerManager>();
            if (_listenerManager == null) return;

            _listenerManager.Attach(Camera.main.transform);
        }

        private void LateUpdate()
        {
            // 移動しながら鳴る音用
            if (_soundPlayer == null) return;

            _soundPlayer.UpdateSourcePosition(this.transform.position);
        }
        
        /// <summary> プレイヤー用サウンドの再生 Animation Eventからも使用可 </summary>
        /// <param name="cueName"></param>
        public void PlaySound(string cueName)
        {
            if (!HasInputAuthority) return;

            Play2DSoundLocal(cueName);
            RPC_Play3DSound(this.transform.position, cueName);
        }

        private void Play2DSoundLocal(string cueName)
        {
            CRIAudio.PlaySE(_sheetName, cueName); // 2D再生
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        private void RPC_Play3DSound(Vector3 pos, string cueName, RpcInfo info = default)
        {
            if (info.IsInvokeLocal) return; // 自分に返る場合は音を消す

            CRIAudio.PlaySE(pos, _sheetName, cueName); // 3D再生
        }
    }
}