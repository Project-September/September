using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace September.Common
{
    /// <summary>
    /// <see cref="ManualSlider"/>を操作するためのコンポーネント。
    /// ステップ幅や操作間隔の指定が可能。
    /// </summary>
    public class ManualSliderController : MonoBehaviour
    {
        [SerializeField] private float _stepValue = 0.05f;
        [SerializeField] private ManualSlider _slider;
        [SerializeField] private float _holdDelay = 0.5f;
        [SerializeField] private float _repeatInterval = 0.2f;
        
        private void Start()
        {
            _slider.stepSize = _stepValue;
            
            this.ObserveEveryValueChanged(x => x._stepValue).Subscribe(s => _slider.stepSize = s).AddTo(this);
            
            var canceled = Observable.FromEvent<InputAction.CallbackContext>(
                h => GameInput.I.UI.Volume.canceled += h,
                h => GameInput.I.UI.Volume.canceled -= h)
                .AsUnitObservable();

            var inactive = this.ObserveEveryValueChanged(x => CanProcess()).Where(v => !v).AsUnitObservable();

            var finish = inactive.Merge(canceled);
            
            this
                .UpdateAsObservable()
                .Where(_ => CanProcess())
                .Select(_ => ReadDir())
                .DistinctUntilChanged()
                .Where(v => v != 0)
                .Select(_ =>
                {
                    MoveSlider();

                    return Observable.Timer(TimeSpan.FromSeconds(_holdDelay))
                        .TakeUntil(finish)
                        .SelectMany(_ => 
                            Observable.Interval(TimeSpan.FromSeconds(_repeatInterval))
                                .TakeUntil(finish)
                        );
                })
                .Switch()
                .Subscribe(_ => MoveSlider())
                .AddTo(this);
        }
        
        private bool CanProcess()
        {
            if (!_slider || !EventSystem.current) return false;
            if (EventSystem.current.currentSelectedGameObject != _slider.gameObject) return false;
            if (!_slider.gameObject.activeInHierarchy) return false;

            return true;
        }

        private float ReadDir()
        {
            var currentInput = GameInput.I.UI.Volume.ReadValue<Vector2>().x;
            var dir = currentInput == 0 ? 0 : currentInput > 0 ? 1 : -1;
            
            return dir;
        }

        private void MoveSlider()
        {
            var currentInput = GameInput.I.UI.Volume.ReadValue<Vector2>().x;

            if (currentInput == 0) return;
            
            var dir = currentInput > 0 ? 1 : -1;
            var moveValue = dir * _stepValue;
            
            _slider.value = Mathf.Clamp01(_slider.value + moveValue);
        }
    }
}