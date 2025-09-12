using System;
using System.Threading;
using Cysharp.Threading.Tasks; // 追加
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

        [Header("Landing")] [SerializeField] private AnimationClip _landing;

        [Tooltip("Landing の保持時間。_useLandingClipLength=true の場合は無視され、クリップ長を使います。")] [SerializeField, Range(0f, 1.5f)]
        private float _landingHoldTime = 0.22f;

        [SerializeField] private bool _useLandingClipLength = true;
        [SerializeField, Range(0.1f, 2f)] private float _landingLengthScale = 1f;

        // ▼ 追加: ブレンド設定
        [Header("Fall Blend")] [SerializeField, Range(0f, 1f)]
        private float _fallInTime = 0.20f;

        [SerializeField] private AnimationCurve _fallInCurve = null; // null の場合は線形扱い
        [SerializeField, Range(0f, 1f)] private float _landOutTime = 0.12f;
        [SerializeField] private AnimationCurve _landOutCurve = null;

        private bool _isVaultingLastFrame = false;
        private bool _isFadingOutFall = false; // 着地フェード多重起動防止

        private void LateUpdate()
        {
            if (!_animationClipPlayer) return;

            var maxSpeed = _playerMovement.DashMoveSpeed;
            var walkSpeed = _playerMovement.WalkSpeed;
            var moveSpeed = _playerMovement.MoveVelocity.magnitude;

            var weight = (moveSpeed <= walkSpeed)
                ? Mathf.InverseLerp(0f, walkSpeed, moveSpeed) // 0..1
                : Mathf.InverseLerp(walkSpeed, maxSpeed, moveSpeed) + 1f; // 1..2

            if (Mathf.Abs(weight) < 1e-3f) weight = 0f;
            _animationClipPlayer.SetLocoWeight(Mathf.Clamp(weight, 0f, 2f));

            // Vault: 再生/解除
            if (_playerMovement.DoingVault && !_isVaultingLastFrame)
            {
                _isVaultingLastFrame = true;
                StartTopLayerInstant(_jumpOver);
            }
            else if (!_playerMovement.DoingVault && _isVaultingLastFrame)
            {
                _isVaultingLastFrame = false;
                ClearTopLayerClip();
            }

            // ===== 落下の開始（TopLayerへFallDown挿入→重み0→1へブレンド） =====
            if (!_playerMovement.IsGroundNet
                && !_animationClipPlayer.IsPlayingTargetClip(_jumpOver)
                && !_animationClipPlayer.IsPlayingTargetClip(_fallDown))
            {
                var topToken = _animationClipPlayer.EnsureLayerToken(LayerInfo.LayerType.TopLayer, this.GetCancellationTokenOnDestroy());

                _animationClipPlayer.SetTopPriorityClip(_fallDown);
                _animationClipPlayer.SetLayerWeight(LayerInfo.LayerType.TopLayer, 0f);
                _animationClipPlayer.BlendLayerWeight(
                    LayerInfo.LayerType.TopLayer,
                    1f,
                    new LayerInfo.Blend { BlendTime = _fallInTime, BlendCurve = _fallInCurve }
                , topToken).Forget();

                _isFadingOutFall = false;
            }

            // ===== 着地（FallDown→Landing→フェードアウト） =====
            if (_playerMovement.IsGroundNet && _animationClipPlayer.IsPlayingTargetClip(_fallDown))
            {
                if (!_isFadingOutFall)
                {
                    _isFadingOutFall = true;
                    // 着地シーケンス開始
                    StartLandingSequence().Forget();
                }
            }
        }

        /// <summary>TopLayerに即時クリップ差し替え（重みは変更しない）</summary>
        private void StartTopLayerInstant(AnimationClip clip)
        {   
            _animationClipPlayer.EnsureLayerToken(LayerInfo.LayerType.TopLayer, this.GetCancellationTokenOnDestroy());
            _animationClipPlayer.SetTopPriorityClip(clip);
        }

        /// <summary>TopLayerクリップ解除（重みは触らない）</summary>
        private void ClearTopLayerClip()
        {
            _animationClipPlayer.SetTopPriorityClip(null);
        }

        /// <summary>
        /// FallDown中に地面へ接地したら、Landingを再生してからTopLayerをフェードアウトして解除。
        /// 落下→着地の“繋ぎ”を一つのシーケンスとして実行する。
        /// </summary>
        private async UniTaskVoid StartLandingSequence()
        {
            var topToken = _animationClipPlayer.EnsureLayerToken(LayerInfo.LayerType.TopLayer, this.GetCancellationTokenOnDestroy());
            var token = _animationClipPlayer.EnsureLayerToken(LayerInfo.LayerType.TopLayer, this.GetCancellationTokenOnDestroy());
            try
            {
                // まだTopLayerの重みは1のはずだが、保険で1へ寄せる（ごく短いブレンド）
                // await _animationClipPlayer.BlendLayerWeight(
                //     LayerInfo.LayerType.TopLayer,
                //     1f,
                //     new LayerInfo.Blend { BlendTime = 0.06f, BlendCurve = null }
                // , token).AttachExternalCancellation(token);

                if (_landing != null)
                {
                    _animationClipPlayer.SetTopPriorityClip(_landing);

                    // 再落下やヴォルト開始で中断される可能性があるので、待ちはCT付きで
                    var hold = _useLandingClipLength && _landing.length > 0f
                        ? _landing.length * Mathf.Max(0.01f, _landingLengthScale)
                        : _landingHoldTime;

                    await UniTask.Delay(TimeSpan.FromSeconds(hold),
                        DelayType.DeltaTime, PlayerLoopTiming.Update, token);
                }

                // 1 → 0 へブレンドして解除
                await _animationClipPlayer.BlendLayerWeight(
                    LayerInfo.LayerType.TopLayer,
                    0f,
                    new LayerInfo.Blend { BlendTime = _landOutTime, BlendCurve = _landOutCurve }
                ).AttachExternalCancellation(token);

                ClearTopLayerClip();
            }
            catch (OperationCanceledException)
            {
                // 中断（再落下/ヴォルト等）→ 何もしない（新しい状態が優先）
            }
        }
    }
}