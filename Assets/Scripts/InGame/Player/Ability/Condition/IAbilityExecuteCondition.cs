using System;
using Fusion;
using UnityEngine;

namespace  InGame.Player.Ability
{
    /// <summary>
    /// 与えられた情報を元にどの入力でどのアビリティを発動するかの条件を定義するインターフェース
    /// </summary>
    public interface IAbilityExecuteCondition
    {
        public string TargetAbilityName { get; }
        public bool IsConditionMatch(in TriggerEventContext context);
    }

    public readonly struct TriggerEventContext
    {
        public GameObject Owner { get; }
        public AbilityBase AbilityRef { get; }
        public NetworkButtons CurrentButtons { get; }
        public NetworkButtons PreviousButtons { get; }

        public TriggerEventContext(GameObject owner, AbilityBase abilityRef, NetworkButtons currentButtons, NetworkButtons previousButtons)
        {
            Owner = owner;
            AbilityRef = abilityRef;
            CurrentButtons = currentButtons;
            PreviousButtons = previousButtons;
        }
    }
}
