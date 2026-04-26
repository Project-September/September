using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using InGame.Player;
using UniRx;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    public class StatsManager : NetworkBehaviour
    {
        [SerializeField] private PlayerParameter _params;
        
        private StatsContainer _baseStats;
        private StatusPipeline _pipeline;
        
        private readonly StatusEffectManager _statusEffectManager = new();

        protected StatsContainer CurrentStats;
        
        private Dictionary<StatType, Stat> _statsTable;
        private Dictionary<StatType, Subject<float>> _statOnChangedSubjectsTable;

        private void Awake()
        {
            _baseStats = _params.GetStats();
            _pipeline = new StatusPipeline(_statusEffectManager);
            _statOnChangedSubjectsTable = _baseStats.Stats.ToDictionary(s => s.Key, _ => new Subject<float>());
        }

        private void FixedUpdate()
        {
            CurrentStats = _pipeline.CalcStats(_baseStats);
        }
        
        private bool TryGetSubject(StatType statType, out Subject<float> subject)
        {
            return _statOnChangedSubjectsTable.TryGetValue(statType, out subject);
        }

        private void OnNextStatOnChanged(StatType statType, float value)
        {
            if (TryGetSubject(statType, out var subject))
            {
                subject.OnNext(value);
            }
        }

        public void SubscribeStatOnChanged(StatType statType, Action<float> action)
        {
            if (TryGetSubject(statType, out var subject))
            {
                subject.Subscribe(action);
            }
        }

        public void SetBaseValue(StatType statType, float value)
        {
            _baseStats.TrySetStatValue(statType, value);
            OnNextStatOnChanged(statType, value);
        }

        public void AddBaseValue(StatType statType, float value)
        {
            _baseStats.TryAddStatValue(statType, value);
            OnNextStatOnChanged(statType, value);
        }
    }
}