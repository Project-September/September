using System;
using Fusion;
using InGame.Player.Ult;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ability.Condition
{
    [Serializable]
    public class AbilityUltCondition : IAbilityExecuteCondition
    {
        [SerializeField] private string _targetAbilityName;
        [SerializeField] private PlayerButtons _button = PlayerButtons.Ability3;
        public string TargetAbilityName => _targetAbilityName;
        
        public bool IsConditionMatch(in TriggerEventContext context)
        {
            if (!context.CurrentButtons.GetPressed(context.PreviousButtons).IsSet(_button) ||
                !PlayerDatabase.Instance.PlayerDataDic.TryGet(NetworkRunner.Instances[0].LocalPlayer, out _))
                return false;

            if (!context.Owner.TryGetComponent<IUltCondition>(out var condition) ||
                !condition.IsAvailable()) 
                return false;
            
            condition.OnUltActivated();
            
            return true;
        }
    }
}