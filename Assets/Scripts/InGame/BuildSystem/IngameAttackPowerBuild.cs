using September.Common;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    public sealed class IngameAttackPowerBuild : BuildSystem
    {
        AttackPowerBuildRuntime _runtime;

        public override bool Init()
        {
            if (_buildData == null) return false;
            if (_buildData is not AttackPowerBuild) return false;
            _runtime = _buildData.CreateBuildInstance() as AttackPowerBuildRuntime;
            if (_runtime == null) return false;
            return true;
        }

        public override void Apply(ref StatsContainer stats)
        {
            stats.TryAddStatValue(StatType.AttackDamage, _runtime.CurrentBuild);
        }

        public override void UpdateBuild(float value)
        {
            if (_enable)
            {
                _runtime.UpdateBuild((int)value);
#if UNITY_EDITOR
                Debug.Log(_runtime.CurrentBuild);
#endif
            }
        }
    }
}
