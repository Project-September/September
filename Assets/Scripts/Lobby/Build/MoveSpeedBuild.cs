using UnityEngine;

namespace September.Lobby
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
