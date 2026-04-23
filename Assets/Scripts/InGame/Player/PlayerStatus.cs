using System.Collections.Generic;
using Codice.CM.Common.Tree.Partial;
using Fusion;

using InGame.Common;
using UniRx;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerStatus : EffectableStatus
    {
        [SerializeField] ParameterData[] _parameters;
        //OnChangedÇ≈ïœÇÌÇ¡ÇΩStatÇ…í ímÇëóÇÈÇÃÇ∆ílÇïœçXÇ∑ÇÈ
        [Networked, OnChangedRender(nameof(OnStatsChanged))] private NetworkArray<NetworkStat> Stats => default;

        [Networked, HideInInspector, OnChangedRender(nameof(OnChangeHealth))] public int CurrentHealth { get; set; }
        private void OnChangeHealth() => _currentHealth.Value = CurrentHealth;
        [Networked, HideInInspector, OnChangedRender(nameof(OnChangeStamina))] public float CurrentStamina { get; set; }
        private void OnChangeStamina() => _currentStamina.Value = CurrentStamina;

        private Dictionary<StatType, Stat> _stats = new();


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
            foreach (var parameter in _parameters)
            {
                _stats.Add(parameter.Type,new Stat(parameter,ActiveEffectSpecs));
            }

            //MaxHealth = _param.Health;
            //CurrentHealth = MaxHealth;
            //MaxStamina = _param.Stamina;
            //CurrentStamina = _param.Stamina;
            //StaminaRegen = _param.StaminaRegen;
            //StaminaConsumption = _param.StaminaConsumption;
            //AttackDamage = _param.AttackDamage;
        }

        //private void OnStatsChanged(ChangedCache<PlayerStatus> changed)
        private void OnStatsChanged()
        {
            //var self = changed.Behaviour;

            //for (int i = 0; i < self.Stats.Length; i++)
            //{
            //    var newStat = self.Stats[i];
            //    var oldStat = changed.LoadOld().Stats[i];

            //    if (!newStat.Equals(oldStat))
            //    {
            //        self._stats[(StatType)i].SetFromNetwork(newStat.Value);
            //    }
            //}
        }

        protected override void OnPostApplyEffect(ActiveStatusEffect activeEffect)
        {
            foreach (var modifier in activeEffect.Spec.Modifiers)
            {
                if (!TryGetStatFromType(modifier.StatType, out var stat)) return;

                stat.SetValue(stat.Value);

            }
        }
        protected override void OnNextValue(StatType type)
        {
            if (!TryGetStatFromType(type, out var stat)) return;

            if (stat.IsValid)
            {
                stat.OnPostApplyEffect.OnNext(stat.Value);
            }
        }
        protected override bool TryGetStatFromType(StatType type, out Stat stat)
        {
            if(_stats.TryGetValue(type, out stat)) return true;

            base.TryGetStatFromType(type,out stat);
            return false;
        }
    }
}