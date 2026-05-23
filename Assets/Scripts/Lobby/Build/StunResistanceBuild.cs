using September.InGame.Common.Stats;
using UnityEngine;

namespace September.Common
{
    [CreateAssetMenu(fileName = "StunResistanceBuild", menuName = "Build/StunResistanceBuild")]
    public class StunResistanceBuild : BuildParamBase<int, float>
    {
        public override IBuild CreateBuildInstance()
        {
            return new StunResistanceBuildRuntime(this);
        }
    }
}
