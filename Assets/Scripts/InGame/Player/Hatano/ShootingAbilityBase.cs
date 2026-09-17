using System;
using InGame.Common;
using September.Common;
using UnityEngine;
using Fusion;

namespace InGame.Player.Ability.Effect.Shooting
{
    [Serializable]
    public abstract class ShootingAbilityBase : AbilityBase
    {
        [Header("AnimationClipPlayer"), SerializeField] private AnimationClipPlayer _animationClipPlayer;
        [Header("AimCameraController"), SerializeField] protected AimCameraController _aimCameraController;
        //現在と最後の射撃ステートを比較して状態の管理を行う
        [Header("射撃ステート（現在）"), SerializeField] protected ShootingStateType _shootingType;
        [Header("射撃ステート（最後）"), SerializeField] protected ShootingStateType _lastShootingType;
        [Header("射撃Abilityの設定")]
        [Header("射撃距離"), SerializeField] protected float _shootingDistance;
        [Header("マズル（数に応じて追加）"), SerializeField] protected Transform[] _muzzlePos;
        [Header("AnimationClip")]
        [Header("構え"), SerializeField] private AnimationClip _stanceAnimationClip;
        [Header("撃つ"), SerializeField] private AnimationClip _shootAnimationClip;
        [Header("ヒット対象レイヤー"), SerializeField] protected LayerMask _hitLayerMask = ~0;
        
        [Header("射撃判定から除外するコライダー（自身は自動除外）"), SerializeField]
        private Collider[] _excludedColliders = Array.Empty<Collider>();
        private readonly RaycastHit[] _shotHits = new RaycastHit[16];

        private PlayerManager _playerManager;

        private NetworkBool _isShootingAnimation; // 射撃アニメーションを再生済みか

        protected virtual bool ReplayAnimationOnEveryShot => false;
        
        protected override void OnStart()
        {
            if(_playerManager == null)
                _playerManager = Parameter.Owner.GetComponent<PlayerManager>();
            
            _animationClipPlayer.SetAim(true);
            _animationClipPlayer.PlayOnUpperBody(_stanceAnimationClip, _animationClipPlayer.AimBlendDuration);
            ApplyCameraState(ShootingStateType.Stance);
        }

        /// <summary>
        /// 射撃などの入力を判定する
        /// </summary>
        protected void ShootingInputJudgment()
        {
            if (_playerManager.IsStun)
            {
                ForceEndAbility();
                return;
            }

            // 構え解除と同時の射撃を受け付けない。
            if (!_playerInput.Buttons.IsSet(PlayerButtons.Ability2))
            {
                _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
                ApplyCameraState(ShootingStateType.None);
                ResetShootingState();
                EndAnimation(blend: true);
                return;
            }

            // 回避は構えAbilityを終了しない。カメラと構えを維持し、
            // 攻撃入力と遠距離インタラクトの進行だけを止める。
            if (_playerManager.GetComponent<PlayerMovement>().IsEvading)
            {
                OnNoShooting();
                _isShootingAnimation = false;
                return;
            }

            //射撃ステートが構えの場合、射撃入力を受け付ける
            if (_shootingType == ShootingStateType.Stance)
            {
                //射撃入力時、継承先ごとの射撃処理を行う
                if (_playerInput.Buttons.IsSet(PlayerButtons.Attack))
                {
                    OnShooting();
                    if (ReplayAnimationOnEveryShot || !_isShootingAnimation)
                    {
                        _isShootingAnimation = true;
                        _animationClipPlayer.PlayOnUpperBody(_shootAnimationClip);
                    }
                   
                }
                else //射撃入力がされていないときに行う処理
                {
                    OnNoShooting();
                    _isShootingAnimation = false;
                }
            }

        }

        // 切替先のカメラや上半身モーションには触れず、旧武器の処理を終了する。
        protected void ResetShootingState()
        {
            _phase = AbilityPhase.Available;
            _shootingType = ShootingStateType.None;
            _lastShootingType = ShootingStateType.None;
            _isShootingAnimation = false;
            OnStopTheStance();
        }

        protected bool StopIfControlLocked()
        {
            if (_playerManager.CurrentPlayerControlState == PlayerManager.PlayerControlState.Normal)
                return false;

            _aimCameraController.StopAim();
            ResetShootingState();
            // Ult や展示物側が設定したカメラ・操作状態・モーションを上書きしない。
            if (_animationClipPlayer.IsCurrentClipOnLayer(LayerInfo.LayerType.UpperBody, _stanceAnimationClip)
                || _animationClipPlayer.IsCurrentClipOnLayer(LayerInfo.LayerType.UpperBody, _shootAnimationClip))
                _animationClipPlayer.PlayOnUpperBody(null);
            _animationClipPlayer.SetAim(false);
            return true;
        }
        
        /// <summary>
        /// 射撃した位置を取得する（カメラからのRay）
        /// </summary>
        /// <param name="origin">カメラの位置</param>
        /// <param name="direction">カメラのforward</param>
        /// <param name="layerMask">省略時は Inspector のヒット対象レイヤーを使用</param>
        /// <param name="excludedColliders">省略時は Inspector の除外コライダーを使用。自身は常に除外</param>
        /// <returns>最寄りの有効な命中位置。命中しなければ最大射程の位置</returns>
        protected Vector3 ShootingPositionDetection(Vector3 origin, Vector3 direction,
            LayerMask? layerMask = null, Collider[] excludedColliders = null)
        {
            var hit = TryGetShotHit(origin, direction, out var hitInfo, layerMask, excludedColliders);
            return hit ? hitInfo.point : origin + direction * _shootingDistance;
        }
        
        // 照準と銃口で同じ対象レイヤー・自身除外を適用し、エフェクトの終点も一致させる。
        protected bool TryGetShotHit(Vector3 origin, Vector3 direction, out RaycastHit hit,
            LayerMask? layerMask = null, Collider[] excludedColliders = null)
        {
            var mask = layerMask ?? _hitLayerMask;
            var exclusions = excludedColliders ?? _excludedColliders;
            var hits = _shotHits;
            var count = Physics.RaycastNonAlloc(origin, direction, hits, _shootingDistance, mask);
            // バッファ満杯時は最寄りの有効なヒットが含まれる保証がないため、全件取得する。
            if (count == hits.Length)
            {
                hits = Physics.RaycastAll(origin, direction, _shootingDistance, mask);
                count = hits.Length;
            }

            hit = default;
            var nearestDistance = float.PositiveInfinity;
            // ローカル照準プレビューは Ability.Start より前にも呼ばれる。
            var owner = Parameter?.Owner;
            if (owner == null && _aimCameraController != null)
                owner = _aimCameraController.GetComponentInParent<NetworkObject>();
            for (var i = 0; i < count; i++)
            {
                var candidate = hits[i];
                if (candidate.collider == null
                    || (owner != null && (candidate.collider.transform.IsChildOf(owner.transform)
                        || candidate.collider.GetComponentInParent<NetworkObject>() == owner))
                    || (exclusions != null && Array.IndexOf(exclusions, candidate.collider) >= 0)
                    || candidate.distance >= nearestDistance)
                    continue;

                hit = candidate;
                nearestDistance = candidate.distance;
            }

            return hit.collider != null;
        }

        /// <summary>
        /// 射撃ステートの状態を検知する
        /// カメラの切替を行う
        /// </summary>
        protected void StateDetection()
        {
            if (_shootingType != _lastShootingType)
            {
                _lastShootingType = _shootingType;
                ApplyCameraState(_shootingType == ShootingStateType.None
                    ? ShootingStateType.None : ShootingStateType.Stance);
            }
        }

        /// <summary>
        /// カメラの切替を行う
        /// Stance：Aimカメラに変更
        /// None：Normalカメラに変更
        /// </summary>
        /// <param name="type">射撃ステート</param>
        protected void ApplyCameraState(ShootingStateType type)
        {
            if (type == ShootingStateType.Stance)
            {
                _aimCameraController.RPC_AimCamera();
                _aimCameraController.RPC_CrosshairToggleChange(true);
            }
            else
            {
                _aimCameraController.RPC_NormalCamera();
                _aimCameraController.RPC_CrosshairToggleChange(false);
            }
        }

        protected override void OnEndAbility()
        {
            EndStance();
            EndAnimation();
        }

        /// <summary>
        /// Ends the stance for ability completion, ult and stun; evasion keeps it active.
        /// Camera/stance cleanup must not unlock the control state owned by respawn or ult.
        /// </summary>
        private void EndStance()
        {
            _shootingType = ShootingStateType.None;
            _lastShootingType = ShootingStateType.None;
            ApplyCameraState(ShootingStateType.None);

            OnStopTheStance();
        }

        /// <summary>
        /// 射撃入力をしたときに行う処理を書く
        /// </summary>
        protected virtual void OnShooting(){}
        
        /// <summary>
        /// 射撃入力が行われていないときに行う処理を書く
        /// </summary>
        protected virtual void OnNoShooting(){}

        /// <summary>
        /// 構え状態が終了したときに行う処理を書く
        /// </summary>
        protected virtual void OnStopTheStance(){}

        /// <summary>
        /// アニメーションを停止
        /// </summary>
        private void EndAnimation(bool blend = false)
        {
            _isShootingAnimation = false;
            _animationClipPlayer.PlayOnUpperBody(null, blend ? _animationClipPlayer.AimBlendDuration : 0f);
            _animationClipPlayer.SetAim(false);
        }
    }
}
