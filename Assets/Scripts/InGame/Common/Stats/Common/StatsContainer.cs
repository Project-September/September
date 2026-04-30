using System;
using Fusion;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    public readonly struct StatsContainer : INetworkStruct
    {
        [Networked, Capacity(16)] public NetworkDictionary<StatType, Stat> Stats => default;

        public StatsContainer(ReadOnlySpan<Stat> stats)
        {
            foreach (var stat in stats)
            {
                Stats.Add(stat.StatType, stat);
            }
        }
        
        public Stat GetStat(StatType type)
        {
            return Stats[type];
        }

        public bool TryGetStat(StatType type, out Stat stat)
        {
            if (Stats.ContainsKey(type))
            {
                stat = Stats[type];
                return true;
            }

            stat = default;
            return false;
        }

        public bool TryGetStatValue(StatType type, out float value)
        {
            if (TryGetStat(type, out var stat))
            {
                value = stat.Value; 
                return true;
            }
            value = 0;
            return false;
        }
        
        public bool TrySetStatValue(StatType type, float value)
        {
            if (Stats.ContainsKey(type))
            {
                SetStatValueInternal(type, value);
                return true;
            }

            return false;
        }

        public bool TryAddStatValue(StatType type, float value)
        {
            if (TryGetStat(type, out var stat))
            {
                SetStatValueInternal(type, stat.Value + value);
                return true;
            }
            
            return false;
        }

        private void SetStatValueInternal(StatType type, float value)
        {
            var newStat = Stats[type];
            newStat.SetValue(value);
            Stats.Set(type, newStat);
        }
    }

    [Serializable]
    public struct Stat : INetworkStruct
    {
        [SerializeField] private StatType _statType;
        public StatType StatType => _statType;

        [Networked] public float Value { get; private set; }

        public float MaxValue;
        public float MinValue;

        public void SetValue(float value)
        {
            Value = Mathf.Clamp(value, MinValue, MaxValue);
        }

        public Stat(StatType statType, float baseValue, float minValue = 0, float maxValue = float.PositiveInfinity)
        {
            _statType = statType;
            Value = baseValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }
}