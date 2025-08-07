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

        public bool IsConditionMatch(TriggerEventContext context)
        {
            //Available状態でAttackボタンが押されたら条件を満たす
            return context.AbilityRef.Phase == AbilityBase.AbilityPhase.Available &&
                   context.AbilityRef.CanStartAbilityOverride() &&
                   context.CurrentButtons.GetPressed(context.PreviousButtons).IsSet(PlayerButtons.Attack);
        }
    }
}
