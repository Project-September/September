using UnityEngine;

namespace September.InGame.Common.Stats
{
    [CreateAssetMenu(fileName = "IngameBuildFactory", menuName = "Build/IngameBuildFactory")]
    public class IngameBuildFactory : ScriptableObject
    {
        [SerializeField] BuildDefinition _definition;

        public IngameBuildPresenter CreateBuild(IngameBuildView view)
        {
            if (view == null || _definition == null) return null;
            return new IngameBuildPresenter(_definition, view);
        }
    }
}
