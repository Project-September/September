using September.InGame.Common.Stats;
using UnityEngine;

namespace September.Common
{
    [CreateAssetMenu(fileName = "MoveSpeedBuild", menuName = "Build/MoveSpeedBuild")]
    public class MoveSpeedBuild : BuildParamBase<float, float>
    {
        public override IBuild CreateBuildInstance()
        {
            return new MoveSpeedBuildRuntime(this);
        }
    }
}
