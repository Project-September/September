using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace September.Lobby
{
    public class ToggleTweenAnimation : MonoBehaviour
    {
        [SerializeField] private Selectable _selectWhenClose;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectOffsetData[] _rectOffsets;
        [SerializeField] private Ease _ease = Ease.OutBack;
        [SerializeField] private bool _startIsActive = true;
        private Vector2[] _initialPositions;
        private UniTask _currentTask;
        public Selectable SelectWhenOpen { get; set; } = null;
        private void Awake()
        {
            _initialPositions = _rectOffsets.Select(r => r.RectTransform.anchoredPosition).ToArray();
            _canvasGroup.alpha = _startIsActive ? 1 : 0;
            _canvasGroup.interactable = _startIsActive;
            _canvasGroup.blocksRaycasts = _startIsActive;
            _openButton.onClick.AddListener(Open);
            _closeButton.onClick.AddListener(Close);
        }

        private void Open()
        {
            if (!_currentTask.Status.IsCompleted()) return;
            List<UniTask> tasks = new();
            tasks.Add(_canvasGroup.DOFade(1, 1.5f).OnComplete(() =>
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                if(SelectWhenOpen) EventSystem.current.SetSelectedGameObject(SelectWhenOpen.gameObject);
            }).ToUniTask()); 
            for (int i = 0; i < _rectOffsets.Length; i++)
            {
                var rectOffset = _rectOffsets[i];
                rectOffset.RectTransform.anchoredPosition += rectOffset.StartOffset;
                tasks.Add(rectOffset.RectTransform.DOAnchorPos(_initialPositions[i], 2f).SetEase(_ease).ToUniTask());
            }
            _currentTask = UniTask.WhenAll(tasks);
        }

        private void Close()
        {
            if (!_currentTask.Status.IsCompleted()) return;
            List<UniTask> tasks = new();
            tasks.Add(_canvasGroup.DOFade(0, 1.5f).OnComplete(() =>
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                EventSystem.current.SetSelectedGameObject(_selectWhenClose.gameObject);
            }).ToUniTask());
            for (int i = 0; i < _rectOffsets.Length; i++)
            {
                var rectOffset = _rectOffsets[i];
                tasks.Add(rectOffset.RectTransform.DOAnchorPos(_initialPositions[i] + rectOffset.StartOffset, 2f).SetEase(_ease).ToUniTask());
            }
            _currentTask = UniTask.WhenAll(tasks);
        }
        [Serializable]
        struct RectOffsetData
        {
            public RectTransform RectTransform;
            public Vector2 StartOffset;
        }
    }
}