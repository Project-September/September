using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace September.InGame
{
    public class FinishAnimation : MonoBehaviour
    {
        [Header("Text Animation")] 
        [SerializeField] private TextMeshProUGUI _finishText;
        [SerializeField] private float _fadeInDuration = 0.6f;
        [SerializeField] private float _scaleDuration = 0.6f;
        [SerializeField] private float _scaleTarget = 1.8f;
        [SerializeField] private Ease _scaleEase = Ease.OutBack;
        [SerializeField] private float _holdDuration = 1.0f;
        [SerializeField] private float _fadeOutDuration = 1.0f;

        public async UniTask Play()
        {
            await ShowTextAnimation(_finishText);
        }
        
        private async UniTask ShowTextAnimation(TextMeshProUGUI target)
        {
            target.gameObject.SetActive(true);
            target.color = new Color(target.color.r, target.color.g, target.color.b, 0f);
            target.transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            seq.Append(target.DOFade(1f, _fadeInDuration))
                .Join(target.transform.DOScale(_scaleTarget, _scaleDuration).SetEase(_scaleEase))
                .AppendInterval(_holdDuration)
                .Append(target.DOFade(0f, _fadeOutDuration));

            await seq.AsyncWaitForCompletion();
        }
    }
}