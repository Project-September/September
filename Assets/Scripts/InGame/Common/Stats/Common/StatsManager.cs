using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using InGame.Player;
using UniRx;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    /// <summary>
    /// ステータスを管理するクラス
    /// </summary>
    public abstract class StatsManager : NetworkBehaviour
    {
        [SerializeField] private StatsModifierBase[] _modifiers;
        
        private StatsContainer _baseStats;
        private StatusPipeline _pipeline;

        protected StatsContainer CurrentStats;
        

        private StatsContainer _prevStats;
        
        private Dictionary<StatType, Subject<float>> _statOnChangedSubjectsTable;
        
        /// <summary>
        /// ステータスの種類や値を初期化するために使用する
        /// </summary>
        protected abstract StatsContainer GetInitialStats();

        public override void Spawned()
        {
            _baseStats = GetInitialStats();
            CurrentStats = GetInitialStats();
            _pipeline = new StatusPipeline(_modifiers);
            _statOnChangedSubjectsTable = _baseStats.Stats.ToDictionary(s => s.Key, _ => new Subject<float>());
        }

        public override void FixedUpdateNetwork()
        {
            CalcStats();
        }

        private void CalcStats()
        {
            CurrentStats = _pipeline.CalcStats(_baseStats);
            
            foreach (var (statType, prevStat) in _prevStats.Stats)
            {
                if (CurrentStats.TryGetStatValue(statType, out float value) && !Mathf.Approximately(prevStat.Value, value))
                {
                    OnNextStatOnChanged(statType, CurrentStats.Stats[statType].Value);
                }
            }

            _prevStats = CurrentStats;
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
            CalcStats();
        }

        public void AddBaseValue(StatType statType, float value)
        {
            _baseStats.TryAddStatValue(statType, value);
            CalcStats();
        }
    }
}