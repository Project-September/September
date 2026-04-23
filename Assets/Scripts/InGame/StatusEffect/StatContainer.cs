using InGame.Common;
using System.Collections.Generic;
using InGame.Player;
using UnityEngine;
namespace InGame.StatusEffect
{
    /// <summary>
    /// ステータスのデータStatを持つ
    /// </summary>
    public class StatContainer
    {
        private Dictionary<StatType, Stat> _stats = new();
        public StatContainer(ParameterData[] parameters)
        {
            foreach (var data in parameters)
            {
                _stats.Add(data.Type, new Stat(data));
            }
        }
        private Stat GetStat(StatType type)
        {
            if(_stats.TryGetValue(type, out Stat stat)) return stat;

            Debug.LogError()
        }
    }
}
