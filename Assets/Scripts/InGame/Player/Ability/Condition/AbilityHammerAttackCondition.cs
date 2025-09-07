using System;
using InGame.Player.Ability.Effect;
using September.Common;

namespace InGame.Player.Ability.Condition
{
    [Serializable]
    public class AbilityHammerAttackCondition : IAbilityExecuteCondition
    {
        public string TargetAbilityName => nameof(AbilityHammerAttack);

        public bool IsConditionMatch(in TriggerEventContext context)
        {
            //Available状態でAttackボタンが押されたら条件を満たす
            return context.AbilityRef.Phase == AbilityBase.AbilityPhase.Available &&
                   context.AbilityRef.CanStartAbilityOverride() &&
                   context.CurrentButtons.GetPressed(context.PreviousButtons).IsSet(PlayerButtons.Attack);
        }
    }
}
