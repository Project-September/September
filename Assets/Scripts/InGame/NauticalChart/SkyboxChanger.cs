using Fusion;
using UnityEngine;

namespace September.InGame.NauticalChart
{
    public class SkyboxChanger : NetworkBehaviour
    {
        [SerializeField] private Material _stormSkyboxMaterial;

        private Material _originalSkyboxMaterial;

        /// <summary> Skyboxを変更する </summary>
        public void SkyBoxChangeStorm()
        {
            _originalSkyboxMaterial = RenderSettings.skybox;
            RenderSettings.skybox = _stormSkyboxMaterial;
        }

        public void RestoreSkyBox()
        {
            RenderSettings.skybox = _originalSkyboxMaterial;
        }
    }
}
