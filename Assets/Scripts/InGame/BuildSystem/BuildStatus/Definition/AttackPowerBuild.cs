using September.InGame.Common.Stats;
using UnityEngine;

namespace September.Common
{
    [CreateAssetMenu(fileName = "AttackPowerBuild", menuName = "Build/AttackPowerBuild")]
    public class AttackPowerBuild : BuildParamBase<int, int>
    {
        public override IBuild CreateBuildInstance()
        {
            return new AttackPowerBuildRuntime(this);
        }
    }
}
