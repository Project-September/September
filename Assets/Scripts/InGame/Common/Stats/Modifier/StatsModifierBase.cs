using Fusion;

namespace September.InGame.Common.Stats
{
    /// <summary>
    /// ステータスを変化させるクラス
    /// </summary>
    public abstract class StatsModifierBase : NetworkBehaviour
    {
        public abstract StatsContainer Apply(StatsContainer stats);
    }
}