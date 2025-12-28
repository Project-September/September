using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace September.NewResult
{
    public class FadeTransitionView : SceneTransitionView
    {
        [SerializeField] private float _fadeDuration = 0.5f; 
        [SerializeField] private Image _fadePanel;
        
        protected override async UniTask FadeInPanel()
        {
            _fadePanel.gameObject.SetActive(true);
            var c = _fadePanel.color;
            await DOTween.To(() => 1f, x => _fadePanel.color = new Color(c.r, c.g, c.b, x), 0f, _fadeDuration);
            _fadePanel.gameObject.SetActive(false);
        }

        protected override async UniTask FadeOutPanel()
        {
            _fadePanel.gameObject.SetActive(true);
            var c = _fadePanel.color;
            await DOTween.To(() => 0f, x => _fadePanel.color = new Color(c.r, c.g, c.b, x), 1f, _fadeDuration);
        }
        
        protected override async UniTask FadeInPanel(UniTask backgroundTask)
        {
            _fadePanel.gameObject.SetActive(true);
            var c = _fadePanel.color;
            c.a = 1.0f;
            _fadePanel.color = c;
            await backgroundTask;
            await DOTween.To(() => 1f, x => _fadePanel.color = new Color(c.r, c.g, c.b, x), 0f, _fadeDuration);
            _fadePanel.gameObject.SetActive(false);
        }
    }
}