using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace September.InGame
{
    public class ChangeTagOverlayMessage : MonoBehaviour
    {
        [SerializeField] private RectMask2D _rectMask;
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private ChangeTagNoticeData[] _messageList =
        {
            new("鬼に選ばれた", new Color(1f, 0.3f, 0.36f)), 
            new("鬼に選ばれなかった", new Color(0.3f, 1f, 0.47f)), 
            new("鬼になりました", new Color(1f, 0.3f, 0.36f))
        }; 
        [SerializeField] private Ease _showEase = Ease.Linear; 
        [SerializeField] private Ease _hideEase = Ease.Linear;
        [SerializeField] private float _showDuration = 1f;
        [SerializeField] private float _waitDuration = 1f;
        [SerializeField] private float _hideDuration = 1f;
        private UniTask _currentTask;
        
        public void ChangeTagNotice(int messageIndex)
        {
            if (!_currentTask.Status.IsCompleted()) return;
            _currentTask = ChangeTagNoticeTask(messageIndex).Preserve();;
        }
        private async UniTask ChangeTagNoticeTask(int messageIndex)
        {
            if (messageIndex >= _messageList.Length || messageIndex < 0) return;
            var padding = _rectMask.padding;
            _messageList[messageIndex].ApplyText(_text);
            await DOVirtual.Float(1000f, 0f, _showDuration, t =>
            {
                padding[0] = t;
                padding[2] = t;
                _rectMask.padding = padding;
            }).SetEase(_showEase);
            await UniTask.WaitForSeconds(_waitDuration);
            await DOVirtual.Float(0f, 1000f, _hideDuration, t =>
            {
                padding[0] = t;
                padding[2] = t;
                _rectMask.padding = padding;
            }).SetEase(_hideEase);
        }
        [Serializable]
        private struct ChangeTagNoticeData
        {
            public string Message;
            public Color Color;

            public ChangeTagNoticeData(string message, Color color)
            {
                Message = message;
                Color = color;
            }

            public void ApplyText(TextMeshProUGUI text)
            {
                text.text = Message;
                text.color = Color;
            }
        }
    }
}