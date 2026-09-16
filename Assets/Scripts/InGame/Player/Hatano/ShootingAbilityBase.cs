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
        
        private PlayerManager _playerManager;

        private NetworkBool _isShootingAnimation; // 射撃アニメーションを再生済みか

        protected virtual bool ReplayAnimationOnEveryShot => false;
        
        protected override void OnStart()
        {
            if(_playerManager == null)
                _playerManager = Parameter.Owner.GetComponent<PlayerManager>();
            
            _animationClipPlayer.SetAim(true);
            _animationClipPlayer.PlayOnUpperBody(_stanceAnimationClip);
        }

        /// <summary>
        /// 射撃などの入力を判定する
        /// </summary>
        protected void ShootingInputJudgment()
        {
            if (_playerManager.IsStun || _playerManager.GetComponent<PlayerMovement>().IsEvading)
            {
                RequestEndAbility();
                return;
            }

            // 構え解除と同時の射撃を受け付けない。
            if (!_playerInput.Buttons.IsSet(PlayerButtons.Ability2))
            {
                _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
                ApplyCameraState(ShootingStateType.None);
                ResetShootingState();
                EndAnimation();
                return;
            }

            //射撃ステートが構えの場合、射撃入力を受け付ける
            if (_shootingType == ShootingStateType.Stance)
            {
                //射撃入力時、継承先ごとの射撃処理を行う
                if (_playerInput.Buttons.IsSet(PlayerButtons.Shooting))
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
        /// <returns>hit:true　Raycastで当たった場所　hit:false　最大距離</returns>
        protected Vector3 ShootingPositionDetection(Vector3 origin, Vector3 direction)
        {
            var hit = Physics.Raycast(origin, direction, out RaycastHit hitInfo, _shootingDistance);
            return hit ? hitInfo.point : origin + direction * _shootingDistance;
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
                ApplyCameraState(ShootingStateType.Stance);
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
        /// Ends the stance from every exit path (button release, evasion, ult and stun).
        /// The control state is restored only while this stance still owns the aim state;
        /// this prevents a cancelled gun from unlocking an ult that just took control.
        /// </summary>
        private void EndStance()
        {
            bool wasAiming = _aimCameraController != null && _aimCameraController.IsAim;
            _shootingType = ShootingStateType.None;
            _lastShootingType = ShootingStateType.None;
            ApplyCameraState(ShootingStateType.None);

            if (wasAiming && !_playerManager.IsStun)
                _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);

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
        private void EndAnimation()
        {
            _isShootingAnimation = false;
            _animationClipPlayer.PlayOnUpperBody(null);
            _animationClipPlayer.SetAim(false);
        }
    }
}
