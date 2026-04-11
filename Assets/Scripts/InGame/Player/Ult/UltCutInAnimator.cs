using Cinemachine;
using Common.Extensions;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Common;
using UnityEngine;
using UnityEngine.Playables;

namespace InGame.Player.Ult
{
    /// <summary>
    /// 必殺技用カットインの実装クラス
    /// </summary>
    [RequireComponent(typeof(AnimationClipPlayer))]
    public class UltCutInAnimator : CutInAnimatorBase
    {
        [SerializeField] private PlayableDirector _playableDirector;
        [SerializeField] private CinemachineVirtualCameraBase _camera;

        public override void RequestPlayCutInAnimation()
        {
            if (!_playableDirector)
            {
                Debug.LogWarning("[UltCutInAnimator] PlayableDirectorが設定されていません。必殺技カットインはスキップされます");
                return;
            }

            if (!_camera)
            {
                Debug.LogWarning("[UltCutInAnimator] Cameraが設定されていません。必殺技カットインはスキップされます");
                return;
            }
            
            IsCutInAnimationPlaying = true;
            
            RPC_PlayCutInAnimation();
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayCutInAnimation()
        {
            // 発動者かどうかで演出を振り分け
            if (HasInputAuthority)
            {
                PlayLocal().Forget();
            }
            else
            {
                PlayRemote().Forget();
            }
        }

        /// <summary>
        /// 発動者側に合わせてカットインの終了をサーバーに通知する
        /// </summary>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_StopCutInAnimation()
        {
            IsCutInAnimationPlaying = false;
        }

        /// <summary>
        /// 発動者以外が見る演出
        /// </summary>
        private async UniTask PlayRemote()
        {
            await _playableDirector.PlayAsync();
        }

        /// <summary>
        /// 発動者が見る演出
        /// </summary>
        private async UniTask PlayLocal()
        {
            _camera.gameObject.SetActive(true);
            
            await _playableDirector.PlayAsync();
            
            _camera.gameObject.SetActive(false);
            RPC_StopCutInAnimation();
        }
    }
}