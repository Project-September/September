using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.Health; // 追加
using InGame.Player;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace InGame.Common
{
    public class AnimationClipPlayerManager : MonoBehaviour
    {
        [SerializeField] private AnimationClipPlayer _animationClipPlayer;
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private AnimationClip _jumpOver;
        [SerializeField] private float _jumpOverDuration = 0.2f;
        [SerializeField] private AnimationClip _fallDown;
        [SerializeField] private AnimationClip _faint;  // 追加: 気絶アニメーション
        [SerializeField] private AnimationClip _getUp;   // 追加: 起き上がりアニメーション
        [SerializeField] private PlayerManager _playerManager; // 追加: PlayerManager参照
        [SerializeField] private PlayerHealth _playerHealth;

        // ▼ 追加: ブレンド設定
        [Header("Loco Blend")]
        [SerializeField] private float _locoBlendSpeed;
        [Header("Fall Blend")]
        [SerializeField, Range(0f, 1f)] private float _fallInTime = 0.20f;
        [SerializeField] private AnimationCurve _fallInCurve = null;   // null の場合は線形扱い
        [SerializeField, Range(0f, 1f)] private float _landOutTime = 0.12f;
        [SerializeField] private AnimationCurve _landOutCurve = null;
        
        [Header("被ダメ")]
        [SerializeField] private AnimationClip _hitReactionClip = null;
        [Header("気絶")]
        [SerializeField, Range(0f, 1f)] private float _overrideInTime = 0.08f;
        [SerializeField] private AnimationCurve _overrideInCurve = null;
        [SerializeField, Range(0f, 1f)] private float _overrideOutTime = 0.10f;
        [SerializeField] private AnimationCurve _overrideOutCurve = null;
        private bool _hardOverride = false;
        
        private bool _isFadingOutFall = false;       // 着地フェード多重起動防止
        private CancellationTokenSource _overrideCts;
        private bool _isFainting = false;          // 気絶中フラグ（多重起動防止）
        private float _locoWeight;
        private CancellationTokenSource _jumpOverTokenSrc;

        private void Start()
        {
            _playerManager.ObserveEveryValueChanged(x => x.IsStun)
                .Subscribe(isStun =>
                {
                    if (isStun && !_isFainting)
                    {
                        _isFainting = true;
                        TriggerFaint();
                    }
                    else
                    {
                        _isFainting = false;
                    }
                });
            _playerHealth.OnHitTaken += (hitData) =>
            {
                if (!_playerManager.IsStun && hitData.HitActionType == HitActionType.Damage)
                {
                    //被ダメのアニメーション再生
                    _animationClipPlayer.PlayClip(_hitReactionClip);
                }
            };
            
            _playerMovement.OnStartVault += () =>
            {
                if (!_hardOverride)
                {
                    _ = TriggerVault();
                }
            };
            
            _playerMovement.UpdateAsObservable()
                .Select(_ => _playerMovement.IsGround)
                .DistinctUntilChanged().Subscribe(SetFallAnim).AddTo(this);
        }

        private void SetFallAnim(bool isGround)
        {
            if (!isGround
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
            if (!isGround || !_animationClipPlayer.IsPlayingTargetClip(_fallDown)) return;
            if (_isFadingOutFall) return;
            _isFadingOutFall = true;
            //topレイヤーにFallアニメーションがある場合は除外する
            if (_animationClipPlayer.IsPlayingTargetClip(_fallDown)) _animationClipPlayer.SetTopPriorityClip(null);
            FadeOutAndClearFall().Forget();
        }

        public bool EnableFallMotion = true;

        private void LateUpdate()
        {
            if (!_animationClipPlayer) return;

            var maxSpeed = _playerMovement.DashMoveSpeed;
            var walkSpeed = _playerMovement.WalkSpeed;
            var moveSpeed = _playerMovement.NetworkVelocity.magnitude;
            var wishWeight = (moveSpeed <= walkSpeed)
                ? Mathf.InverseLerp(0f, walkSpeed, moveSpeed)         // 0..1
                : Mathf.InverseLerp(walkSpeed, maxSpeed, moveSpeed)+1f; // 1..2
            if (Mathf.Abs(wishWeight) < 1e-3f) wishWeight = 0f;
            // weight を遷移させる
            float deltaWeight = _locoBlendSpeed * Time.deltaTime;
            _locoWeight = Mathf.Abs(_locoWeight - wishWeight) <= deltaWeight ? wishWeight : _locoWeight < wishWeight ? _locoWeight + deltaWeight : _locoWeight - deltaWeight;
            _animationClipPlayer.SetLocoWeight(Mathf.Clamp(_locoWeight, 0f, 2f));
        }
        
        public void TriggerFaint()
        {
            // すでに実行中なら差し替え（再発動）
            _overrideCts?.Cancel();
            _ = PlayFaintSequenceAsync();
        }

        public async UniTask TriggerVault()
        {
            _animationClipPlayer.SetTopPriorityClip(_jumpOver);
            
            if (_jumpOverTokenSrc != null)
            {
                _jumpOverTokenSrc.Cancel();
                _jumpOverTokenSrc.Dispose();
            }
            
            _jumpOverTokenSrc = new CancellationTokenSource();
            Debug.LogError("ジャンプ開始！ JumpOver length: " + _jumpOver.length);
            // ジャンプオーバークリップの長さだけ待機（速度変更を考慮しない場合は length をそのまま使用）
            if (_jumpOver && _jumpOver.length > 0f)
            {
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_jumpOverDuration), cancellationToken: _jumpOverTokenSrc.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
            Debug.LogError("ジャンプ終了！");
            _animationClipPlayer.SetTopPriorityClip(null);
            if (!_playerMovement.IsGround) SetFallAnim(false);
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