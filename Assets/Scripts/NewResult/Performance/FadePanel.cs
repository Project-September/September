using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace September.NewResult
{
    public class FadePanel : SingletonMonoBehaviour<FadePanel>
    {
        public readonly ReactiveProperty<bool> IsFading = new();
        
        [SerializeField] private Image _fadePanel;
        [SerializeField] private float _fadeDuration;

        private void Start()
        {
            DontDestroyOnLoad(this);
        }

        private async UniTask FadeInPanel()
        {
            IsFading.Value = true;
            _fadePanel.gameObject.SetActive(true);
            var c = _fadePanel.color;
            await DOTween.To(() => 1f, x => _fadePanel.color = new Color(c.r, c.g, c.b, x), 0f, _fadeDuration);
            _fadePanel.gameObject.SetActive(false);
            IsFading.Value = false;
        }
        
        private async UniTask FadeOutPanel()
        {
            IsFading.Value = true;
            _fadePanel.gameObject.SetActive(true);
            var c = _fadePanel.color;
            await DOTween.To(() => 0f, x => _fadePanel.color = new Color(c.r, c.g, c.b, x), 1f, _fadeDuration);
            IsFading.Value = false;
        }

        public static async UniTask FadeIn()
        {
            await I.FadeInPanel();
        }

        public static async UniTask FadeOut()
        {
            await I.FadeOutPanel();
        }
    }
}