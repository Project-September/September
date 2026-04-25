using InGame.Common;
using September.InGame.Common.Stats;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerStatus : EffectableStatus
    {
        [SerializeField] PlayerParameter _param;

        private Stat[] _stats;

        protected override Stat[] Stats => _stats;

        // Todo: Statの実体がEffectableStatus側にしかないので、いちいち取ってくる必要があるのめんどくさいね
        public int MaxHealth => (int)GetStat(StatType.Health).MaxValue;
        public int CurrentHealth { get => (int)GetStatValue(StatType.Health); set => SetStatValue(StatType.Health, value); }
        public float MaxStamina => GetStat(StatType.Stamina).MaxValue;
        public float CurrentStamina { get => GetStat(StatType.Stamina).Value; set => SetStatValue(StatType.Stamina, value); }
        public float StaminaRegen => GetStat(StatType.StaminaRegen).Value;
        public float StaminaConsumption => GetStat(StatType.StaminaConsumption).Value;
        public float Speed => GetStat(StatType.Speed).Value;
        public float AttackDamage => GetStat(StatType.AttackDamage).Value;

        protected override void Init()
        {
            _stats = new Stat[]
            {
                new(StatType.Health, _param.Health, ActiveEffectSpecs, maxValue: _param.Health),
                new(StatType.Speed, _param.Speed, ActiveEffectSpecs),
                new(StatType.Stamina, _param.Stamina, ActiveEffectSpecs, maxValue: _param.Stamina),
                new(StatType.StaminaRegen, _param.StaminaRegen, ActiveEffectSpecs),
                new(StatType.StaminaConsumption, _param.StaminaConsumption, ActiveEffectSpecs),
                new(StatType.AttackDamage, _param.AttackDamage, ActiveEffectSpecs),
            };
        }

        protected override void OnPostApplyEffect(ActiveStatusEffect activeEffect)
        {
            foreach (var modifier in activeEffect.Spec.Modifiers)
            {
                if (TryGetStat(modifier.StatType, out var stat))
                {
                    SetStatValue(stat.StatType, stat.Value);
                }
            }
        }
    }
}