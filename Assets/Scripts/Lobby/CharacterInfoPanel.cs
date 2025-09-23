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

        private void Awake()
        {
            _initialPositions = _rectTransforms.Select(rect => rect.anchoredPosition).ToArray();
        }

        public async UniTaskVoid FadeOut()
        {
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
                await UniTask.WaitForSeconds(_delay);
            }
        }

        public async UniTask FadeIn(string characterName,string abilityName, string abilityExplain)
        {
            ApplyContents(characterName, abilityName, abilityExplain);
            for (var i = 0; i < _rectTransforms.Length; i++)
            {
                var pos = _initialPositions[i];
                pos.x = _moveValue;
                _rectTransforms[i].anchoredPosition = pos;
                _rectTransforms[i].DOAnchorPos(_initialPositions[i], 1.5f).SetEase(_easeType);
                _canvasGroups[i].DOFade(1, 2);
                await UniTask.WaitForSeconds(_delay);
            }

            await UniTask.WaitForSeconds(2);
        }

        public void ApplyContents(string characterName, string abilityName, string abilityExplain)
        {
            if (_characterName) _characterName.text = characterName;
            if (_abilityNameText) _abilityNameText.text = abilityName;
            if (_abilityExplainText) _abilityExplainText.text = abilityExplain;
        }
    }
}