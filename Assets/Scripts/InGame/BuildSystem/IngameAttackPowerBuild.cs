using September.Common;

namespace September.InGame.Common.Stats
{
    public sealed class IngameAttackPowerBuild : IngameBuildSystem<AttackPowerBuild, AttackPowerBuildRuntime>
    {
        public override void Apply(ref StatsContainer stats)
        {
            stats.TryAddStatValue(StatType.AttackDamage, _runtime.CurrentBuild);
        }

        public override void UpdateBuild(float value)
        {
            if (_isEnable)
                _runtime.UpdateBuild((int)value);
        }
    }
}
