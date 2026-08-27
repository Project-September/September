using Fusion;
using UnityEngine;

namespace September.InGame.NauticalChart
{
    public class SkyboxChanger : NetworkBehaviour
    {
        [SerializeField] private Material _stormSkyboxMaterial;

        private Material _originalSkyboxMaterial;

        /// <summary> Skyboxを変更する </summary>
        public void SkyboxChangeStorm()
        {
            _originalSkyboxMaterial = RenderSettings.skybox;
            RenderSettings.skybox = _stormSkyboxMaterial;
        }

        /// <summary>
        /// 元のSkyboxに戻す
        /// </summary>
        public void RestoreSkybox()
        {
            RenderSettings.skybox = _originalSkyboxMaterial;
        }
    }
}
