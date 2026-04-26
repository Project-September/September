using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    public readonly struct StatsContainer
    {
        private readonly Dictionary<StatType, Stat> _statsTable;
        
        public IReadOnlyDictionary<StatType, Stat> Stats => _statsTable;

        public StatsContainer(Stat[] stats)
        {
            _statsTable = stats.ToDictionary(stat => stat.StatType, stat => stat);
        }

        public StatsContainer(IReadOnlyDictionary<StatType, Stat> stats)
        {
            _statsTable = stats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        
        public Stat GetStat(StatType type)
        {
            return _statsTable[type];
        }

        public bool TryGetStat(StatType type, out Stat stat)
        {
            return _statsTable.TryGetValue(type, out stat);
        }

        public float GetStatValue(StatType type)
        {
            return _statsTable[type].Value;
        }

        public bool TryGetStatValue(StatType type, out float value)
        {
            if (_statsTable.TryGetValue(type, out var stat))
            {
                value = stat.Value; 
                return true;
            }
            value = 0;
            return false;
        }
        
        public void SetStatValue(StatType type, float value)
        {
            SetStatValueInternal(type, value);
        }

        public void AddStatValue(StatType type, float value)
        {
            SetStatValueInternal(type, _statsTable[type].Value + value);
        }
        
        public bool TrySetStatValue(StatType type, float value)
        {
            if (_statsTable.ContainsKey(type))
            {
                SetStatValueInternal(type, value);
                return true;
            }

            return false;
        }

        public bool TryAddStatValue(StatType type, float value)
        {
            if (_statsTable.TryGetValue(type, out var stat))
            {
                SetStatValueInternal(type, stat.Value + value);
                return true;
            }
            
            return false;
        }

        private void SetStatValueInternal(StatType type, float value)
        {
            var newStat = _statsTable[type];
            newStat.SetValue(value);
            _statsTable[type] = newStat;
        }
    }

    public struct Stat
    {
        public readonly StatType StatType;

        private float _value;

        public float Value
        {
            get => _value;
            set => _value = Mathf.Clamp(value, MinValue, MaxValue);
        }

        public float MaxValue;
        public float MinValue;

        public void SetValue(float value)
        {
            Value = Mathf.Clamp(value, MinValue, MaxValue);
        }

        public Stat(StatType statType, float baseValue, float minValue = 0, float maxValue = float.PositiveInfinity)
        {
            StatType = statType;
            _value = baseValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }
}