using September.Common;

namespace September.InGame.Common.Stats
{
    public class IngameFastInteractBuild : IngameBuildSystem<FastInteractBuild, FastInteractBuildRuntime>
    {
        public override void Apply(ref StatsContainer stats)
        {
            var statType = StatType.InteractDurationMultiply;
            if (!stats.TryGetStatValue(statType, out var interact)) return;
            // 初期パラメータにビルドによる減衰を施す
            // ビルドが0.2になっていたら元の数値の0.8倍になる
            stats.TrySetStatValue(statType, interact * (1 - _runtime.CurrentBuild));
        }

        public override void UpdateBuild(float value)
        {
            if (_isEnable)
                _runtime.UpdateBuild((int)value);
        }
    }
}
