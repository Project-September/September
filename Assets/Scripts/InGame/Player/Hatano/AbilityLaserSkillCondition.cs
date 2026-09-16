using System;
using InGame.Player.Hatano;
using September.Common;

namespace InGame.Player.Ability
{
    [Serializable]
    public class AbilityLaserSkillCondition : IAbilityExecuteCondition
    {
        public string TargetAbilityName => nameof(AbilityLaserGun);

        // キャッシュ用
        private PlayerManager _playerManager;
        private HatanoAbilityStatusManagement _abilityStatusManagement;

        public bool IsConditionMatch(in TriggerEventContext context)
        {
            if (!context.Owner) return false;
            if (!_playerManager) _playerManager = context.Owner.GetComponent<PlayerManager>();

            if (_abilityStatusManagement == null) _abilityStatusManagement = 
                context.Owner.GetComponent<HatanoAbilityStatusManagement>();
            if (!_abilityStatusManagement ||
                _abilityStatusManagement.AbilityStatus != HatanoAbilityStatus.LaserGun) return false;

            // 条件を定義
            return _playerManager.CurrentPlayerControlState == PlayerManager.PlayerControlState.Normal
                   && !_playerManager.IsStun
                   && !_abilityStatusManagement.IsChangingWeapon
                   && !context.CurrentButtons.IsSet(PlayerButtons.Ultimate)
                   && context.AbilityRef.Phase == AbilityBase.AbilityPhase.Available
                   && context.AbilityRef.CanStartAbilityOverride()
                   && context.CurrentButtons.IsSet(PlayerButtons.Ability2);
        }
    }
}

