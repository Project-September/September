using System;
using Fusion;
using InGame.Common;
using UniRx;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerStatus : EffectableStatus
    {
        [SerializeField] PlayerParameter _param;
        
        public int MaxHealth { get; private set; }
        [Networked, HideInInspector, OnChangedRender(nameof(OnChangeHealth))] public int CurrentHealth { get; set; }
        private void OnChangeHealth() => _currentHealth.Value = CurrentHealth;
        public float MaxStamina { get; private set; }
        public float MaxSpeedRate { get; private set; } = 1;
        [Networked, HideInInspector, OnChangedRender(nameof(OnChangeStamina))] public float CurrentStamina { get; set; }
        private void OnChangeStamina() => _currentStamina.Value = CurrentStamina;
        public float StaminaRegen { get; private set; }
        public int AttackDamage { get; set; }

        private Stat _speedStat;
        private Stat _staminaStat;
        private Stat _staminaRegenStat;
        private Stat _attackDamageStat;
        
        #region Events

        private readonly ReactiveProperty<int> _currentHealth = new(0);
        private readonly ReactiveProperty<float> _currentStamina = new(0);
        public ReadOnlyReactiveProperty<int> ReactiveCurrentHealth => _currentHealth.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<float> ReactiveCurrentStamina => _currentStamina.ToReadOnlyReactiveProperty();
        
        #endregion

        public override void Spawned()
        {
            InitStatus();
        }

        void InitStatus()
        {
            MaxHealth = _param.Health;
            CurrentHealth = MaxHealth;
            MaxStamina = _param.Stamina;
            CurrentStamina = _param.Stamina;
            StaminaRegen = _param.StaminaRegen;
            AttackDamage = _param.AttackDamage;

            if (HasStateAuthority)
            {
                _speedStat = new Stat(_param.Speed, ActiveEffectSpecs);
                _staminaStat = new Stat(_param.Stamina, ActiveEffectSpecs);
                _staminaRegenStat = new Stat(_param.StaminaRegen, ActiveEffectSpecs);
                _attackDamageStat = new Stat(_param.AttackDamage, ActiveEffectSpecs);
                
                _speedStat.OnValueChanged.Subscribe(value => MaxSpeedRate = value).AddTo(this);
                _staminaStat.OnValueChanged.Subscribe(value => CurrentStamina = value).AddTo(this);
                _staminaRegenStat.OnValueChanged.Subscribe(value => StaminaRegen = value).AddTo(this);
                _attackDamageStat.OnValueChanged.Subscribe(value => AttackDamage = (int)value).AddTo(this);
            }
        }

        protected override void OnPostApplyEffect(ActiveStatusEffect activeEffect)
        {
            foreach (var modifier in activeEffect.Spec.Modifiers)
            {
                switch (modifier.StatType)
                {
                    case StatType.Speed:
                        _speedStat.SetValue(Mathf.Max(0, _speedStat.Value));
                        break;
                    case StatType.Stamina:
                        _staminaStat.SetBaseValue(Mathf.Clamp(_staminaStat.Value, 0, MaxStamina));
                        break;
                    case StatType.StaminaRegen:
                        _staminaRegenStat.SetValue(Mathf.Max(0, _staminaRegenStat.Value));
                        break;
                    case StatType.AttackDamage:
                        _attackDamageStat.SetValue(Mathf.Max(0, _attackDamageStat.Value));
                        break;
                }
            }
        }

        public override ref Stat GetStatFromType(StatType type)
        {
            //if (type == StatType.Health) return ref _healthStat;
            if (type == StatType.Speed) return ref _speedStat;
            if (type == StatType.Stamina) return ref _staminaStat;
            if (type == StatType.StaminaRegen) return ref _staminaRegenStat;
            if (type == StatType.AttackDamage) return ref _attackDamageStat;
            return ref base.GetStatFromType(type);
        }
    }
}