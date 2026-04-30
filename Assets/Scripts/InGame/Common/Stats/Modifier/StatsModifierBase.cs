using System;
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
    public ref struct EffectableStats
    {
        public Span<Stat> Stats;

        public EffectableStats(Span<Stat> stats) => Stats = stats;

        public void WriteStats(ref StatsContainer container)
        {
            foreach (var stat in Stats)
            {
                container.TrySetStatValue(stat.StatType, stat.Value);
            }
        }
    }
}