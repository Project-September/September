using UnityEngine;

namespace September.Common
{
    [CreateAssetMenu(fileName = "MoveSpeedBuild", menuName = "Build/MoveSpeedBuild")]
    public class MoveSpeedBuild : BuildParamBase<float>
    {
        public override IBuild CreateBuildInstance()
        {
            return null;
        }
    }
}
