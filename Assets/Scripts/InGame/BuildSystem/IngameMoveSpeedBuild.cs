using September.Common;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    public class IngameMoveSpeedBuild : IngameBuildSystem<MoveSpeedBuild, MoveSpeedBuildRuntime>
    {
        public override void Apply(ref StatsContainer stats)
        {
            stats.TryAddStatValue(StatType.Speed, _runtime.CurrentBuild);
        }

        public override void UpdateBuild(float value)
        {
            if (_isEnable)
                _runtime.UpdateBuild(value);
        }
    }
}
