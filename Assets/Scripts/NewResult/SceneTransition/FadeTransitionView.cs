using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace September.NewResult
{
    public class FadeTransitionView : SceneTransitionView
    {
        [SerializeField] private float _fadeDuration = 0.5f;
        [SerializeField] private Ease _ease = Ease.Linear;
        [SerializeField] private Image _fadePanel;
        
        protected override async UniTask FadeInPanel(UniTask loadingTask)
        {
            _fadePanel.gameObject.SetActive(true);
            await Transition(1f, 0f, _fadeDuration, loadingTask);
            _fadePanel.gameObject.SetActive(false);
        }
        
        protected override async UniTask FadeOutPanel(UniTask loadingTask)
        {
            _fadePanel.gameObject.SetActive(true);
            await Transition(0f, 1f, _fadeDuration, loadingTask);
        }

        private async UniTask Transition(float from, float to, float duration, UniTask loadingTask)
        {
            var c = _fadePanel.color;
            c.a = from;
            _fadePanel.color = c;
            await loadingTask;
            await _fadePanel.DOFade(to, duration).SetEase(_ease);
        }
    }
}