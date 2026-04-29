using UnityEngine;

namespace September.Lobby
{
    [CreateAssetMenu(fileName = "FastInteractBuild", menuName = "Build/FastInteractBuild")]
    public class FastInteractBuild : BuildParamBase<float>
    {
        public override IBuild CreateBuildInstance()
        {
            throw new System.NotImplementedException();
        }
    }
}
