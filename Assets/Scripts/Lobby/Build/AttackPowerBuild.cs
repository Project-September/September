using UnityEngine;

namespace September.Lobby
{
    [CreateAssetMenu(fileName = "AttackPowerBuild", menuName = "Build/AttackPowerBuild")]
    public class AttackPowerBuild : BuildParamBase<int>
    {
        public override IBuild CreateBuildInstance()
        {
            return null;
        }
    }
}
