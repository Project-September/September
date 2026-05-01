using Fusion;

namespace September.InGame.Common.Stats
{
    /// <summary>
    /// バフシステムなどの、ステータスを変化させるシステムの基底クラス
    /// </summary>
    public abstract class StatsEffectorBase : NetworkBehaviour
    {
        public abstract void Apply(ref StatsContainer stats);
    }
}