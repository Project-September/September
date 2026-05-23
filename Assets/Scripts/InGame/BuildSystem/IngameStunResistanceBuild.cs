using September.Common;

namespace September.InGame.Common.Stats
{
    public class IngameStunResistanceBuild : IngameBuildSystem<StunResistanceBuild, StunResistanceBuildRuntime>
    {
        public override void Apply(ref StatsContainer stats)
        {
            var statType = StatType.StunDurationMultiply;
            if (!stats.TryGetStatValue(statType, out var stun)) return;
            // 初期パラメータにビルドによる減衰を施す
            // ビルドが0.2になっていたら元の数値の0.8倍になる
            stats.TrySetStatValue(statType, stun * (1 - _runtime.CurrentBuild));
        }

        public override void UpdateBuild(float value)
        {
            if (_enable)
                _runtime.UpdateBuild((int)value);
        }
    }
}
