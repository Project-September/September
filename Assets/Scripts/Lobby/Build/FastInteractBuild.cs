using UnityEngine;
using September.InGame.Common.Stats;

namespace September.Common
{
    [CreateAssetMenu(fileName = "FastInteractBuild", menuName = "Build/FastInteractBuild")]
    public class FastInteractBuild : BuildParamBase<int, float>
    {
        public override IBuild CreateBuildInstance()
        {
            return new FastInteractBuildRuntime(this);
        }
    }
}
