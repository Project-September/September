using System.Collections.Generic;
using Fusion;
using InGame.Common;
using UnityEngine;
using September.Common;

namespace InGame.Player.Hatano
{
    /// <summary>
    /// ハタノ・エリクソンのAbility選択状態を管理する
    /// </summary>
    public class HatanoAbilityStatusManagement : NetworkBehaviour
    {
        [SerializeField] private AnimationClipPlayer _animClipPlayer;
        [SerializeField] private HatanoChangeAnimationController _changeAnimation;
        [Header("切替アニメーション"), SerializeField] private AnimationClip _changeClip;
        [Header("レーザー銃"), SerializeField] private GameObject _laser;
        [Header("二丁拳銃"), SerializeField] private List<GameObject> _doubles;
        [Header("現在の選択中のAbility")]
        [Networked] private HatanoAbilityStatus _abilityStatus {get; set;}
        public HatanoAbilityStatus AbilityStatus => _abilityStatus;
        public HatanoAbilityStatus _lastAbilityStatus;
        
        private HatanoAbilityStatusUIManager _abilityStatusUIManager;
        private AimCameraController _aimCameraController;
        private bool _isChangeAbilityInput; //Abilityの変更入力

        private void Awake()
        {
            _abilityStatusUIManager = GetComponent<HatanoAbilityStatusUIManager>();
            _aimCameraController = GetComponent<AimCameraController>();
        }

        public override void Spawned()
        {
            _abilityStatus = HatanoAbilityStatus.DoubleBarreledGun;
        }

        public override void FixedUpdateNetwork()
        {
            if (_abilityStatus != _lastAbilityStatus)
            {
                _lastAbilityStatus = _abilityStatus;
                _abilityStatusUIManager.SelectedAbilityUITextChanged(_abilityStatus);
            }
            
            if(!HasInputAuthority) return;
            //入力がなかったら処理を行わない
            if (!GetInput<PlayerInput>(out var input)) return;
            //構えているときはアビリティの変更を行えないようにする
            if(_aimCameraController.IsAim) return;

            if (input.Buttons.IsSet(PlayerButtons.Ability1) && !_isChangeAbilityInput)
            {
                _isChangeAbilityInput = true;
                var next = GetNextHatanoAbilityStatus();

                if (HasStateAuthority)
                {
                    _abilityStatus = next;
                    _animClipPlayer.PlayClip(_changeClip);
                    ChangeAbility(next);
                }
                else
                {
                    RPC_ChangeAbilityStatus(next);
                }
            }

            if (!input.Buttons.IsSet(PlayerButtons.Ability1) && _isChangeAbilityInput) 
                _isChangeAbilityInput = false;
        }

        /// <summary>
        /// 現在のアビリティに応じてアビリティの変更を行う
        /// </summary>
        /// <returns>変更後のアビリティ</returns>
        private HatanoAbilityStatus GetNextHatanoAbilityStatus()
        {
            return _abilityStatus switch
            {
                HatanoAbilityStatus.DoubleBarreledGun => HatanoAbilityStatus.LaserGun,
                HatanoAbilityStatus.LaserGun => HatanoAbilityStatus.DoubleBarreledGun
            };
        }

        /// <summary>
        /// アビリティの変更を行う
        /// </summary>
        /// <param name="status">変更後のアビリティ</param>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_ChangeAbilityStatus(HatanoAbilityStatus status)
        {
            _abilityStatus = status;
            ChangeAbility(status);
        }

        /// <summary>
        /// アビリティの変更
        /// </summary>
        /// <param name="status">変更後のアビリティ</param>
        private void ChangeAbility(HatanoAbilityStatus status)
        {
            // 表示する銃とアニメーションを変更
            switch (status)
            {
                case HatanoAbilityStatus.LaserGun:
                    _laser.SetActive(true);
                    foreach (var d in _doubles) d.SetActive(false);
                    _changeAnimation.ChangeMoveAnimation(status);
                    break;
                case HatanoAbilityStatus.DoubleBarreledGun:
                    _laser.SetActive(false);
                    foreach (var d in _doubles) d.SetActive(true);
                    _changeAnimation.ChangeMoveAnimation(status);
                    break;
            }
        }
    }
}
