using System;
using Fusion;

namespace September.InGame.Common.Stats
{
    /// <summary>
    /// ステータスを変化させるクラス
    /// </summary>
    public abstract class StatsModifierBase : NetworkBehaviour
    {
        public abstract void Apply(CalcStats stats);
    }

    /// <summary>
    /// ステータス計算用のオブジェクト。Spanを使用して高速に計算を行う
    /// </summary>
    public ref struct CalcStats
    {
        public Span<Stat> Stats;

        public CalcStats(Span<Stat> stats) => Stats = stats;

        public void WriteStats(ref StatsContainer container)
        {
            foreach (var stat in Stats)
            {
                container.TrySetStatValue(stat.StatType, stat.Value);
            }
        }
    }
}