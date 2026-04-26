namespace September.InGame.Common.Stats
{
    /// <summary>
    /// ステータスを変化させるクラス
    /// </summary>
    public abstract class StatsModifierBase
    {
        public abstract StatsContainer Apply(StatsContainer stats);
    }
}