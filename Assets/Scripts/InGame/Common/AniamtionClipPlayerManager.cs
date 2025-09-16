using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion.Addons.Physics; // 追加
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
        [SerializeField] private AnimationClip _faint;  // 追加: 気絶アニメーション
        [SerializeField] private AnimationClip _getUp;   // 追加: 起き上がりアニメーション
        [SerializeField] private PlayerManager _playerManager; // 追加: PlayerManager参照

        // ▼ 追加: ブレンド設定
        [Header("Fall Blend")]
        [SerializeField, Range(0f, 1f)] private float _fallInTime = 0.20f;
        [SerializeField] private AnimationCurve _fallInCurve = null;   // null の場合は線形扱い
        [SerializeField, Range(0f, 1f)] private float _landOutTime = 0.12f;
        [SerializeField] private AnimationCurve _landOutCurve = null;
        
        [Header("気絶")]
        [SerializeField, Range(0f, 1f)] private float _overrideInTime = 0.08f;
        [SerializeField] private AnimationCurve _overrideInCurve = null;
        [SerializeField, Range(0f, 1f)] private float _overrideOutTime = 0.10f;
        [SerializeField] private AnimationCurve _overrideOutCurve = null;
        private bool _hardOverride = false;

        private bool _isVaultingLastFrame = false;
        private bool _isFadingOutFall = false;       // 着地フェード多重起動防止
        private CancellationTokenSource _overrideCts;

        public bool EnableFallMotion = true;

        private void LateUpdate()
        {
            if (!_animationClipPlayer) return;

            var maxSpeed = _playerMovement.DashMoveSpeed;
            var walkSpeed = _playerMovement.WalkSpeed;
            var moveSpeed = _playerMovement.NetworkVelocity.magnitude;
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
                && EnableFallMotion
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
        
        public void TriggerFaint()
        {
            // すでに実行中なら差し替え（再発動）
            _overrideCts?.Cancel();
            _ = PlayFaintSequenceAsync();
        }

        /// <summary>強制解除（リスポーン等）</summary>
        public void ForceClearOverride()
        {
            _overrideCts?.Cancel();
            _hardOverride = false;
            _animationClipPlayer.SetTopPriorityClip(null);
            _animationClipPlayer.SetLayerWeight(LayerInfo.LayerType.TopLayer, 0f);
            _overrideCts = null;
        }
        
          private async UniTaskVoid PlayFaintSequenceAsync()
        {
            _hardOverride = true;

            var cts = new CancellationTokenSource();
            _overrideCts = cts;
            try
            {
                // 気絶へフェードイン（TopLayer=1）
                if (_faint != null)
                {
                    _animationClipPlayer.SetTopPriorityClip(_faint);
                }
                _animationClipPlayer.SetLayerWeight(LayerInfo.LayerType.TopLayer, 0f);

                await _animationClipPlayer.BlendLayerWeight(
                    LayerInfo.LayerType.TopLayer,
                    1f,
                    new LayerInfo.Blend { BlendTime = _overrideInTime, BlendCurve = _overrideInCurve }
                );

                // 気絶クリップの長さだけ待機（速度変更を考慮しない場合は length をそのまま使用）
                if (_faint != null && _faint.length > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_faint.length), cancellationToken: cts.Token);
                }

                // 起き上がり（任意）
                if (_getUp != null)
                {
                    _animationClipPlayer.SetTopPriorityClip(_getUp);
                    if (_getUp.length > 0f)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(_getUp.length), cancellationToken: cts.Token);
                    }
                }

                // フェードアウトして解除
                await _animationClipPlayer.BlendLayerWeight(
                    LayerInfo.LayerType.TopLayer,
                    0f,
                    new LayerInfo.Blend { BlendTime = 0, BlendCurve = null }
                );

                _animationClipPlayer.SetTopPriorityClip(null);
                _hardOverride = false;
                _overrideCts = null;
            }
            catch (OperationCanceledException)
            {
                // 再発動や強制解除でキャンセルされた
            }
            finally
            {
                // 途中キャンセル時も確実に状態を畳む
                if (_hardOverride && (cts.IsCancellationRequested))
                {
                    _animationClipPlayer.SetTopPriorityClip(null);
                    _animationClipPlayer.SetLayerWeight(LayerInfo.LayerType.TopLayer, 0f);
                    _hardOverride = false;
                    if (ReferenceEquals(_overrideCts, cts)) _overrideCts = null;
                }
                cts.Dispose();
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
        }
    }
}