using System;
using Fusion;
using InGame.Exhibit;
using UniRx;
using UnityEngine;

namespace September.InGame.UI 
{
    // 各UIのイベントを所持するクラス
    // 登録も自身で行う
    public class UIController : SingletonMonoBehaviour<UIController>
    {
        #region イベント

        private readonly Subject<bool> _onClickOptionButton = new();
        private readonly ReactiveProperty<int> _onChangeSliderValue = new();
        private readonly Subject<NetworkRunner> _onStartTimer = new();
        private readonly Subject<string> _onShowLog = new();
        private readonly ReactiveProperty<bool> _onShowOgreUI = new();
        private readonly ReactiveProperty<float> _onChangeStaminaValue = new();
        private readonly Subject<Unit> _onGameStart = new();
        private readonly Subject<Unit> _onGameEnd = new();
        private readonly Subject<(bool, GameObject)> _isInteracting = new();
        private readonly ReactiveProperty<float> _onChangeInteractProgress = new();
        private readonly Subject<(float,StatusUpType)> _onInteractStatusUpObject = new(); 

        #endregion
        
        # region 外部公開プロパティ
        
        public IObservable<bool> OnClickOptionButton => _onClickOptionButton;
        public IReadOnlyReactiveProperty<int> OnChangeSliderValue => _onChangeSliderValue;
        public IObservable<NetworkRunner> OnStartTimer => _onStartTimer;
        public IObservable<string> OnShowLog => _onShowLog;
        public IObservable<bool> OnShowOgreUI => _onShowOgreUI;
        public IReadOnlyReactiveProperty<float> OnChangeStaminaValue => _onChangeStaminaValue;
        
        public IObservable<Unit> OnGameStart => _onGameStart;
        public IObservable<Unit> OnGameEnd => _onGameEnd;
        public IObservable<(bool, GameObject)> IsInteracting => _isInteracting;
        public IReadOnlyReactiveProperty<float> OnChangeInteractProgress => _onChangeInteractProgress;
        public IObservable<(float,StatusUpType)> OnInteractStatusUpObject => _onInteractStatusUpObject;
        
        #endregion
        
        public void SetUpStartUI()
        {
            _onGameStart.OnNext(Unit.Default);
        }

        public void ShowResultAnimation()
        {
            _onGameEnd.OnNext(Unit.Default);
        }

        public void ShowLog(string text)
        {
            _onShowLog.OnNext(text);
        }
        
        public void StartTimer(NetworkRunner runner)
        {
            _onStartTimer.OnNext(runner);
        }

        public void ShowOgreLamp(bool isShow)
        {
            _onShowOgreUI.Value = isShow;
        }

        public void ChangeSliderValue(int value)
        {
            _onChangeSliderValue.Value = value;
        }

        public void ChangeStaminaValue(float value)
        {
            _onChangeStaminaValue.Value = value;
        }
        public void ShowInteractUI(bool isShow, GameObject target = null)
        {
            _isInteracting.OnNext((isShow, target));
        }
        
        public void SetInteractProgress(float progress)
        {
            _onChangeInteractProgress.Value = progress;
            if (progress >= 1.0f)
            {
                _isInteracting.OnNext((false, null));
            }
        }

        public void ShowStatusUpUI(float seconds, StatusUpType status)
        {
            _onInteractStatusUpObject.OnNext((seconds,status));
        }
    }
}