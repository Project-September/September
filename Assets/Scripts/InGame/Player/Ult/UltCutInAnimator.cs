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
        /// 発動者に合わせてカットインの終了を通知する
        /// </summary>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_StopCutInAnimation()
        {
            IsCutInAnimationPlaying = false;
        }

        /// <summary>
        /// 全員が見る演出
        /// </summary>
        private async UniTask PlayRemote()
        {
            Debug.Log($"<color=cyan> Remote </color>");
            
            await _playableDirector.PlayAsync();
        }

        /// <summary>
        /// 発動者のみの演出
        /// </summary>
        private async UniTask PlayLocal()
        {
            Debug.Log($"<color=cyan> Local </color>");
            
            _camera.gameObject.SetActive(true);
            
            await _playableDirector.PlayAsync();
            
            _camera.gameObject.SetActive(false);
            RPC_StopCutInAnimation();
        }
    }
}