using System;
using Fusion;
using InGame.Player.Ability;
using September.Common;

namespace  InGame.Player.Ability
{
    [Serializable]
    public class AbilityNormalAttackExecuteCondition : IAbilityExecuteCondition
    {
        public string TargetAbilityName => nameof(AbilityNormalAttack);
        private PlayerMovement _playerMovement;
        private PlayerManager _playerManager;

        public bool IsConditionMatch(in TriggerEventContext context)
        {
            if (!context.Owner) return false;
            if (!_playerMovement) _playerMovement = context.Owner.GetComponent<PlayerMovement>();
            if (!_playerManager) _playerManager = context.Owner.GetComponent<PlayerManager>();

            //Available状態でAttackボタンが押されたら条件を満たす
            return _playerMovement.IsGround && !_playerManager.IsStun &&
                   context.AbilityRef.Phase == AbilityBase.AbilityPhase.Available &&
                   context.AbilityRef.CanStartAbilityOverride() &&
                   context.CurrentButtons.GetPressed(context.PreviousButtons).IsSet(PlayerButtons.Attack);
        }
    }
}
