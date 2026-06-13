using UnityEngine;

namespace September.InGame.Common.Stats
{
    [CreateAssetMenu(fileName = "IngameBuildFactory", menuName = "Build/IngameBuildFactory")]
    public class IngameBuildFactory : ScriptableObject
    {
        [SerializeField] BuildDefinition _definition;

        public IngameBuildPresenter CreateBuild(IngameBuildView view, BuildEffector effector)
        {
            if (_definition == null || view == null || effector == null) return null;
            return new IngameBuildPresenter(_definition, view, effector);
        }
    }
}
