using System;
using InGame.Common;
using September.Common;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.Player.Ability.Effect.Shooting
{
    [Serializable]
    public abstract class ShootingAbilityBase : AbilityBase
    {
        [Header("AnimationClipPlayer")]
        [SerializeField] private AnimationClipPlayer _animationClipPlayer;
        [Header("AimCameraController")]
        [SerializeField] protected AimCameraController _aimCameraController;
        //現在と最後の射撃ステートを比較して状態の管理を行う
        [Header("射撃ステート（現在）")]
        [SerializeField] protected ShootingStateType _shootingType;
        [Header("射撃ステート（最後）")]
        [SerializeField] protected ShootingStateType _lastShootingType;
        [Header("射撃Abilityの設定")]
        [Header("射撃距離")]
        [SerializeField] protected float _shootingDistance;
        [Header("マズル（数に応じて追加）")]
        [SerializeField] protected Transform[] _muzzlePos;
        [Header("AnimationClip")]
        [Header("構え")]
        [SerializeField] private AnimationClip _stanceAnimationClip;
        [Header("撃つ")] 
        [SerializeField] private AnimationClip _shootAnimationClip;
        
        private PlayerManager _playerManager;

        protected override void OnStart()
        {
            if(_playerManager == null)
                _playerManager = Parameter.Owner.GetComponent<PlayerManager>();
        }

        /// <summary>
        /// 射撃などの入力を判定する
        /// </summary>
        protected void ShootingInputJudgment()
        {
            _playerManager.SetControlState(PlayerManager.PlayerControlState.InputLocked);
            //射撃ステートが構えの場合、射撃入力を受け付ける
            if (_shootingType == ShootingStateType.Stance)
            {
                //射撃入力時、継承先ごとの射撃処理を行う
                if (_playerInput.Buttons.IsSet(PlayerButtons.Shooting))
                {
                    OnShooting();
                }
                else //射撃入力がされていないときに行う処理
                {
                    OnNoShooting();
                }
                
                PlayAnimation().Forget();
            }

            //構え入力が終了➤構える前の状態に戻す
            if (!_playerInput.Buttons.IsSet(PlayerButtons.Ability2))
            {
                _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
                ApplyCameraState(ShootingStateType.None);
                _phase = AbilityPhase.Available;
                _shootingType = ShootingStateType.None;
                _lastShootingType = ShootingStateType.None;
                OnStopTheStance();
            }
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
            _playerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
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

        private async UniTask PlayAnimation()
        {
            // 構えアニメーションを再生
            if (_shootingType == ShootingStateType.Stance)
            {
                _animationClipPlayer.PlayClip(_stanceAnimationClip);
            }
            else if (_shootingType == ShootingStateType.Shooting) // 撃つアニメーションを再生
            {
                // 撃つアニメーションの再生が終了するまで待機し、構えアニメーションを再生
                var endType = await _animationClipPlayer.PlayClipAndWait(_shootAnimationClip);
                if (endType == EndClipType.Complete)
                {
                    _animationClipPlayer.PlayClip(_stanceAnimationClip);
                }
            }
        }
    }
}
