using UnityEngine;

namespace September.Common
{
    [CreateAssetMenu(fileName = "FastInteractBuild", menuName = "Build/FastInteractBuild")]
    public class FastInteractBuild : BuildParamBase<int, float>
    {
        public override IBuild CreateBuildInstance()
        {
            throw new System.NotImplementedException();
        }
    }
}
