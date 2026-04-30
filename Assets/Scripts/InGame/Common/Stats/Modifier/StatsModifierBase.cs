using System.Collections.Generic;
using Fusion;

namespace September.InGame.Common.Stats
{
    /// <summary>
    /// ステータスを変化させるクラス
    /// </summary>
    public abstract class StatsModifierBase : NetworkBehaviour
    {
        public abstract void Apply(EffectableStats stats);
    }

    /// <summary>
    /// 計算用ステータス
    /// </summary>
    public class EffectableStats
    {
        public readonly Dictionary<StatType, Stat> Stats = new();

        public void SetStats(IEnumerable<KeyValuePair<StatType, Stat>> stats)
        {
            Stats.Clear();
            foreach (var kvp in stats)
            {
                var stat = kvp.Value;
                Stats.Add(stat.StatType, stat);
            }
        }

        public void GetStats(ref StatsContainer container)
        {
            foreach (var (statType, stat) in Stats)
            {
                container.TrySetStatValue(statType, stat.Value);
            }
        }
    }
}