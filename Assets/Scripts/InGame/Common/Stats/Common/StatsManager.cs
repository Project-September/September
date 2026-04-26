using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
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

        /// <summary> バフなどの要因を加味した最終的なステータス </summary>
        protected StatsContainer CurrentStats;

        /// <summary> 変更を検知する用 </summary>
        private StatsContainer _prevStats;
        
        /// <summary> ステータスの変更を外に通知する用 </summary>
        private Dictionary<StatType, Subject<float>> _statOnChangedSubjectsTable;
        
        /// <summary> 変更があったステータスを追跡する用 </summary>
        private readonly HashSet<StatType> _statDirtyFlags = new();
        
        /// <summary>
        /// ステータスの種類や値を初期化するために使用する
        /// </summary>
        protected abstract StatsContainer GetInitialStats();

        protected abstract void OnStatsUpdated(HashSet<StatType> updatedStats);

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

        /// <summary>
        /// ステータスを再計算する
        /// </summary>
        private void CalcStats()
        {
            CurrentStats = _pipeline.CalcStats(_baseStats);
            
            foreach (var (statType, prevStat) in _prevStats.Stats)
            {
                if (CurrentStats.TryGetStatValue(statType, out float value) && !Mathf.Approximately(prevStat.Value, value))
                {
                    OnNextStatOnChanged(statType, CurrentStats.Stats[statType].Value);
                    _statDirtyFlags.Add(statType);
                }
            }

            _prevStats = CurrentStats;
            
            OnStatsUpdated(_statDirtyFlags);
            
            _statDirtyFlags.Clear();
        }
        
        /// <summary>
        /// ステータスの変化を外部に通知する
        /// </summary>
        private void OnNextStatOnChanged(StatType statType, float value)
        {
            if (_statOnChangedSubjectsTable.TryGetValue(statType, out var subject))
            {
                subject.OnNext(value);
            }
        }

        /// <summary>
        /// 指定のステータスが変化したら通知を受け取る
        /// </summary>
        public void SubscribeStatOnChanged(StatType statType, Action<float> action)
        {
            if (_statOnChangedSubjectsTable.TryGetValue(statType, out var subject))
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