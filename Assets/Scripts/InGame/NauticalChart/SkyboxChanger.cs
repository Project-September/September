using DG.Tweening;
using Fusion;
using UnityEngine;

namespace September.InGame.NauticalChart
{
    public class SkyboxChanger : NetworkBehaviour
    {
        private static readonly int Blend = Shader.PropertyToID("_Blend");

        [SerializeField] private Ease _easeType = Ease.InOutSine;
        [SerializeField] private float _duration = 0.5f;

        /// <summary> Skyboxを変更する </summary>
        public void SkyboxChangeStorm()
        {
            var skybox = RenderSettings.skybox;
            DOTween.To(() => skybox.GetFloat(Blend), x => skybox.SetFloat(Blend, x), 1, _duration).SetEase(_easeType);
        }

        /// <summary>
        /// 元のSkyboxに戻す
        /// </summary>
        public void RestoreSkybox()
        {
            var skybox = RenderSettings.skybox;
            DOTween.To(() => skybox.GetFloat(Blend), x => skybox.SetFloat(Blend, x), 0, _duration).SetEase(_easeType);
        }
    }
}
