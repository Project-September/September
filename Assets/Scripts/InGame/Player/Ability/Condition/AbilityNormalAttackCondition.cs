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

        public bool IsConditionMatch(in TriggerEventContext context)
        {
            if (!context.Owner) return false;
            if (!_playerMovement)
                _playerMovement = context.Owner.GetComponent<PlayerMovement>();

            //Available状態でAttackボタンが押されたら条件を満たす
            return _playerMovement.IsGround &&
                   context.AbilityRef.Phase == AbilityBase.AbilityPhase.Available &&
                   context.AbilityRef.CanStartAbilityOverride() &&
                   context.CurrentButtons.GetPressed(context.PreviousButtons).IsSet(PlayerButtons.Attack);
        }
    }
}
