using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace September.Lobby
{
    public class ToggleTweenAnimation : MonoBehaviour
    {
        [SerializeField] Button _openButton;
        [SerializeField] Button _closeButton;
        [SerializeField] CanvasGroup _canvasGroup;
        [SerializeField] RectOffsetData[] _rectOffsets;
        [SerializeField] Ease _ease = Ease.OutBack; 
        Vector2[] _initialPositions;

        private void Awake()
        {
            _initialPositions = _rectOffsets.Select(r => r.RectTransform.anchoredPosition).ToArray();
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _openButton.onClick.AddListener(Open);
            _closeButton.onClick.AddListener(Close);
        }

        private void Open()
        {
            _canvasGroup.DOFade(1, 1.5f).OnComplete(() =>
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            });
            for (int i = 0; i < _rectOffsets.Length; i++)
            {
                var rectOffset = _rectOffsets[i];
                rectOffset.RectTransform.anchoredPosition += rectOffset.StartOffset;
                rectOffset.RectTransform.DOAnchorPos(_initialPositions[i], 2f).SetEase(_ease);
            }
        }

        private void Close()
        {
            _canvasGroup.DOFade(0, 1.5f).OnComplete(() =>
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            });
            for (int i = 0; i < _rectOffsets.Length; i++)
            {
                var rectOffset = _rectOffsets[i];
                rectOffset.RectTransform.DOAnchorPos(_initialPositions[i] + rectOffset.StartOffset, 2f).SetEase(_ease);
            }
        }
        [Serializable]
        struct RectOffsetData
        {
            public RectTransform RectTransform;
            public Vector2 StartOffset;
        }
    }
}