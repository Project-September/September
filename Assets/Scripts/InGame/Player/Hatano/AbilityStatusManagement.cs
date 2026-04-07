using System.Linq.Expressions;
using Fusion;
using UnityEngine;
using September.Common;
using TMPro;

namespace InGame.Player.Hatano
{
    /// <summary>
    /// ハタノ・エリクソンのAbility選択状態を管理する
    /// </summary>
    public class AbilityStatusManagement : NetworkBehaviour
    {
        [Header("現在の選択中のAbility")]
        [Networked] private HatanoAbilityStatus _abilityStatus {get; set;}
        public HatanoAbilityStatus AbilityStatus => _abilityStatus;
        public HatanoAbilityStatus _lastAbilityStatus;
        
        private AbilityStatusUIManager _abilityStatusUIManager;
        private AimCameraController _aimCameraController;
        private bool _isChangeAbilityInput; //Abilityの変更入力

        private void Awake()
        {
            _abilityStatusUIManager = GetComponent<AbilityStatusUIManager>();
            _aimCameraController = GetComponent<AimCameraController>();
        }

        public override void Spawned()
        {
            _abilityStatus = HatanoAbilityStatus.None;
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
                RPC_ChangeAbilityStatus(next);
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
                HatanoAbilityStatus.None => HatanoAbilityStatus.DoubleBarreledGun,
                HatanoAbilityStatus.DoubleBarreledGun => HatanoAbilityStatus.RemoteInteraction,
                HatanoAbilityStatus.RemoteInteraction => HatanoAbilityStatus.RocketLauncher,
                HatanoAbilityStatus.RocketLauncher => HatanoAbilityStatus.DoubleBarreledGun,
                _ => HatanoAbilityStatus.None
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
        }
    }
}
