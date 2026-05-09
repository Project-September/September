using UnityEngine;

namespace September.Common
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
