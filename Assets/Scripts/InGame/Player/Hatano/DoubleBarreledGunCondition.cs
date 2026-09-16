using System;
using InGame.Player.Hatano;
using September.Common;

namespace InGame.Player.Ability
{
    [Serializable]
    public class DoubleBarreledGunCondition : IAbilityExecuteCondition
    {
        public string TargetAbilityName => nameof(DoubleBarreledGun);

        // キャッシュ用
        private PlayerMovement _playerMovement;
        private PlayerManager _playerManager;
        private HatanoAbilityStatusManagement _abilityStatusManagement;
        private AimCameraController _aimCameraController;

        public bool IsConditionMatch(in TriggerEventContext context)
        {
            if (!context.Owner) return false;
            if (!_playerMovement) _playerMovement = context.Owner.GetComponent<PlayerMovement>();
            if (!_playerManager) _playerManager = context.Owner.GetComponent<PlayerManager>();
            if (!_aimCameraController) _aimCameraController = context.Owner.GetComponent<AimCameraController>();
            
            if (_abilityStatusManagement == null) _abilityStatusManagement = 
                context.Owner.GetComponent<HatanoAbilityStatusManagement>();
            if (!_abilityStatusManagement ||
                _abilityStatusManagement.AbilityStatus != HatanoAbilityStatus.DoubleBarreledGun) return false;
            
            // 条件を定義
            return _playerManager.CurrentPlayerControlState == PlayerManager.PlayerControlState.Normal
                   && !_playerManager.IsStun && !_playerMovement.IsEvading
                   && !_abilityStatusManagement.IsChangingWeapon
                   && !context.CurrentButtons.IsSet(PlayerButtons.Ultimate)
                   && context.AbilityRef.Phase == AbilityBase.AbilityPhase.Available
                   && context.AbilityRef.CanStartAbilityOverride()
                   && (context.CurrentButtons.GetPressed(context.PreviousButtons).IsSet(PlayerButtons.Ability2)
                       || (_aimCameraController && _aimCameraController.IsAim
                           && context.CurrentButtons.IsSet(PlayerButtons.Ability2)));
        }
    }
}

