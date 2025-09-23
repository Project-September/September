using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace September.Common
{
    public class ArrowReaction : MonoBehaviour
    {
        [SerializeField] private Image _arrowImage;
        [SerializeField] private Image _selectFrameImage;
        [SerializeField] private Selectable _targetSelectable;
        private Tween _tween;
        protected void Awake()
        {
            if(_arrowImage) _arrowImage.enabled = false;
            if(_selectFrameImage) _selectFrameImage.enabled = false;
            if (_targetSelectable)
            {
                _targetSelectable.OnSelectAsObservable().Subscribe(OnSelect).AddTo(this);
                _targetSelectable.OnDeselectAsObservable().Subscribe(OnDeselect).AddTo(this);
            }
        }

        private void OnSelect(BaseEventData eventData)
        {
            if (_arrowImage)
            {
                _arrowImage.enabled = true;
                _tween = _arrowImage.transform.DORotate(new Vector3(360f, 0f, 0f), 1.5f, RotateMode.LocalAxisAdd).SetLoops(-1, LoopType.Restart).SetEase(Ease.InOutSine).SetLink(gameObject);
            }

            if (_selectFrameImage)
            {
                _selectFrameImage.enabled = true;
            }
        }

        private void OnDeselect(BaseEventData eventData)
        {
            if(_selectFrameImage)
            {
                _selectFrameImage.enabled = false;
            }
            if (!_arrowImage) return;
            _arrowImage.enabled = false;
            _arrowImage.transform.rotation = Quaternion.identity;
            _tween?.Kill();
        }
    }
}