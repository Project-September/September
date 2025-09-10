using Cysharp.Threading.Tasks;           // 追加
using InGame.Player;
using UnityEngine;

namespace InGame.Common
{
    public class AnimationClipPlayerManager : MonoBehaviour
    {
        [SerializeField] private AnimationClipPlayer _animationClipPlayer;
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private AnimationClip _jumpOver;
        [SerializeField] private AnimationClip _fallDown;

        // ▼ 追加: ブレンド設定
        [Header("Fall Blend")]
        [SerializeField, Range(0f, 1f)] private float _fallInTime = 0.20f;
        [SerializeField] private AnimationCurve _fallInCurve = null;   // null の場合は線形扱い
        [SerializeField, Range(0f, 1f)] private float _landOutTime = 0.12f;
        [SerializeField] private AnimationCurve _landOutCurve = null;

        private bool _isVaultingLastFrame = false;
        private bool _isFadingOutFall = false;       // 着地フェード多重起動防止

        private void LateUpdate()
        {
            if (!_animationClipPlayer) return;

            var maxSpeed = _playerMovement.DashMoveSpeed;
            var walkSpeed = _playerMovement.WalkSpeed;
            var moveSpeed = _playerMovement.MoveVelocity.magnitude;
            var weight = (moveSpeed <= walkSpeed)
                ? Mathf.InverseLerp(0f, walkSpeed, moveSpeed)         // 0..1
                : Mathf.InverseLerp(walkSpeed, maxSpeed, moveSpeed)+1f; // 1..2
            if (Mathf.Abs(weight) < 1e-3f) weight = 0f;
            _animationClipPlayer.SetLocoWeight(Mathf.Clamp(weight, 0f, 2f));

            // Vault: 再生/解除
            if (_playerMovement.DoingVault && !_isVaultingLastFrame)
            {
                _isVaultingLastFrame = true;
                _animationClipPlayer.SetTopPriorityClip(_jumpOver);
            }
            else if (!_playerMovement.DoingVault && _isVaultingLastFrame)
            {
                _isVaultingLastFrame = false;
                _animationClipPlayer.SetTopPriorityClip(null);
            }

            // ===== 落下の開始（TopLayerへクリップ挿入→重み0→1へブレンド） =====
            // すでに FallDown が再生中なら何もしない（毎フレーム再生し直さない）
            if (!_playerMovement.IsGroundNet
                && !_animationClipPlayer.IsPlayingTargetClip(_jumpOver)
                && !_animationClipPlayer.IsPlayingTargetClip(_fallDown))
            {
                _animationClipPlayer.SetTopPriorityClip(_fallDown);
                _animationClipPlayer.SetLayerWeight(LayerInfo.LayerType.TopLayer, 0f);
                _animationClipPlayer.BlendLayerWeight(
                    LayerInfo.LayerType.TopLayer,
                    1f,
                    new LayerInfo.Blend {
                        BlendTime = _fallInTime,
                        BlendCurve = _fallInCurve
                    }
                ).Forget();

                _isFadingOutFall = false;
            }

            // ===== 着地の終了（1→0へブレンドしてからTopLayer解除） =====
            if (_playerMovement.IsGroundNet && _animationClipPlayer.IsPlayingTargetClip(_fallDown))
            {
                if (!_isFadingOutFall)
                {
                    _isFadingOutFall = true;
                    FadeOutAndClearFall().Forget();
                }
            }
        }

        // 1→0へブレンド完了後にTopLayerを外す
        private async UniTaskVoid FadeOutAndClearFall()
        {
            await _animationClipPlayer.BlendLayerWeight(
                LayerInfo.LayerType.TopLayer,
                0f,
                new LayerInfo.Blend {
                    BlendTime = _landOutTime,
                    BlendCurve = _landOutCurve
                }
            );
            _animationClipPlayer.SetTopPriorityClip(null);
        }
    }
}
