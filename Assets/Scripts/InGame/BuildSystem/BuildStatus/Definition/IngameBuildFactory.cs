using UnityEngine;
using September.Common;

namespace September.InGame.Common.Stats
{
    [CreateAssetMenu(fileName = "IngameBuildFactory", menuName = "Build/IngameBuildFactory")]
    public class IngameBuildFactory : ScriptableObject
    {
        public IngameBuildPresenter CreateBuild(BuildDefinition definition, IngameBuildView view, BuildEffector effector)
        {
            if (definition == null || view == null || effector == null) return null;
            return new IngameBuildPresenter(definition, view, effector);
        }
    }
}
