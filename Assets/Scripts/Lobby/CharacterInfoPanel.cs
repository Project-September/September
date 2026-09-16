using System.Threading;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

namespace September.Lobby
{
    public class CharacterInfoPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup[] _canvasGroups;
        [SerializeField] private RectTransform[] _rectTransforms;
        [SerializeField] private TextMeshProUGUI _characterName;
        [SerializeField] private TextMeshProUGUI _abilityNameText;
        [SerializeField] private TextMeshProUGUI _abilityExplainText;
        [SerializeField] private float _delay = 0.2f;
        [SerializeField] private float _moveValue = 100;
        [SerializeField] Ease _easeType = Ease.OutBack;
        Vector2[] _initialPositions;
        private CancellationTokenSource _animationCancellation;

        private void Awake()
        {
            _initialPositions = _rectTransforms.Select(rect => rect.anchoredPosition).ToArray();
        }

        public void CancelAnimation()
        {
            // 待機中のループも中断し、古いOnCompleteが新しい表示を消すことを防ぐ。
            _animationCancellation?.Cancel();
            _animationCancellation?.Dispose();
            _animationCancellation = null;
            foreach (var rect in _rectTransforms) if (rect) rect.DOKill();
            foreach (var group in _canvasGroups) if (group) group.DOKill();
        }

        private CancellationToken BeginAnimation()
        {
            CancelAnimation();
            _animationCancellation = new CancellationTokenSource();
            return _animationCancellation.Token;
        }

        private void OnDisable() => CancelAnimation();
        private void OnDestroy() => CancelAnimation();

        public async UniTaskVoid FadeOut()
        {
            var token = BeginAnimation();
            for (var i = 0; i < _rectTransforms.Length; i++)
            {
                var moveTween = _rectTransforms[i].DOAnchorPosX(-_moveValue, 2).SetEase(_easeType);
                var temp = i;
                _canvasGroups[i].DOFade(0, 1).OnComplete(() =>
                {
                    moveTween.Kill();
                    _characterName.text = string.Empty;
                    transform.SetAsFirstSibling();
                });
                if (await UniTask.WaitForSeconds(_delay, cancellationToken: token).SuppressCancellationThrow()) return;
            }
        }

        public async UniTask FadeIn(string characterName,string abilityName, string abilityExplain)
        {
            var token = BeginAnimation();
            foreach (var group in _canvasGroups) group.alpha = 0;
            transform.SetAsLastSibling();
            ApplyContents(characterName, abilityName, abilityExplain);
            for (var i = 0; i < _rectTransforms.Length; i++)
            {
                var pos = _initialPositions[i];
                pos.x = _moveValue;
                _rectTransforms[i].anchoredPosition = pos;
                _rectTransforms[i].DOAnchorPos(_initialPositions[i], 1.5f).SetEase(_easeType);
                _canvasGroups[i].DOFade(1, 2);
                if (await UniTask.WaitForSeconds(_delay, cancellationToken: token).SuppressCancellationThrow()) return;
            }

            await UniTask.WaitForSeconds(2, cancellationToken: token).SuppressCancellationThrow();
        }

        public void ApplyContents(string characterName, string abilityName, string abilityExplain)
        {
            if (_characterName) _characterName.text = characterName;
            if (_abilityNameText) _abilityNameText.text = abilityName;
            if (_abilityExplainText) _abilityExplainText.text = abilityExplain;
        }
    }
}