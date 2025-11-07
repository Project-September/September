using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Exhibit;
using September.Common;
using September.InGame.Common;
using UniRx;
using UnityEngine;

namespace September.InGame.UI 
{
    // 各UIのイベントを所持するクラス
    // 登録も自身で行う
    public class UIPresenter : MonoBehaviour
    {
        #region イベント
        
        private readonly Subject<bool> _optionButtonClicked = new();
        private readonly Subject<Unit> _inGameUIDestroyed = new();
        private readonly Subject<int> _onChangeDescriptionUI = new();
        private readonly ReactiveProperty<int> _onChangeSliderValue = new();
        private readonly Subject<NetworkRunner> _onStartTimer = new();
        private readonly Subject<string> _onShowLog = new();
        private readonly ReactiveProperty<bool> _onShowOgreUI = new();
        private readonly Subject<int> _changeTagNoticeObserver = new();
        private readonly Subject<int> _onchangeScoreText = new();
        private readonly ReactiveProperty<float> _onChangeStaminaValue = new();
        private readonly Subject<Unit> _onGameStart = new();
        private readonly Subject<Unit> _onGameEnd = new();
        private readonly Subject<(bool, GameObject)> _isInteracting = new();
        private readonly ReactiveProperty<float> _onChangeInteractProgress = new();
        //private readonly Subject<(float,StatusUpType)> _onInteractStatusUpObject = new();

        #endregion
        
        # region 外部公開プロパティ
        public IObservable<bool> OptionButtonClicked => _optionButtonClicked;
        public IReadOnlyReactiveProperty<int> OnChangeSliderValue => _onChangeSliderValue;
        public IObservable<NetworkRunner> OnStartTimer => _onStartTimer;
        public IObservable<Unit> InGameUIDestroyed => _inGameUIDestroyed;
        public IObservable<string> OnShowLog => _onShowLog;
        public IObservable<bool> OnShowOgreUI => _onShowOgreUI;
        public IObservable<int> ChangeTagNoticeObserver => _changeTagNoticeObserver;
        public IReadOnlyReactiveProperty<float> OnChangeStaminaValue => _onChangeStaminaValue;
        public IObservable<int> OnChangeDescriptionUI => _onChangeDescriptionUI;
        public IObservable<Unit> OnGameStart => _onGameStart;
        public IObservable<Unit> OnGameEnd => _onGameEnd;
        public IObservable<(bool, GameObject)> IsInteracting => _isInteracting;
        public IReadOnlyReactiveProperty<float> OnChangeInteractProgress => _onChangeInteractProgress;
        
        public Func<TimeMessageType, UniTask> TimeOverlayMessage { get; private set; }
        public IObservable<int> OnChangeScoreText => _onchangeScoreText;
        
        #endregion
        
        public InGameUIRootRefs UIRootRefs { get; set;}
        private InGameStatusView _statusView;
        private CancellationTokenSource _cts;

        private void Start()
        {
            InGameManager manager = StaticServiceLocator.Instance.Get<InGameManager>();
            StaticServiceLocator.Instance.Register(this);
            _cts = new CancellationTokenSource();
            manager.GameEnded += DestroyInGameUI; 
            TimeOverlayMessage += _statusView.TimeOverlayMessage;
        }
        
        private void SetUpStartUI()
        {
            _statusView.SetupUI();
            //_onGameStart.OnNext(Unit.Default);
            //OnGameStart.Subscribe(_ => _statusView.SetupUI()).AddTo(_cts.Token);
        }

        private void ShowResultAnimation()
        {
            _statusView.PlayResultAnimation().Forget();
            //_onGameEnd.OnNext(Unit.Default);
            //OnGameEnd.Subscribe(_ => _statusView.PlayResultAnimation().Forget()).AddTo(_cts.Token);
        }

        private void OnChangeScore(int score)
        {
            _statusView.ChangeScore(score);
            //_onchangeScoreText.OnNext(score);   
            //OnChangeScoreText.Subscribe(_statusView.ChangeScore).AddTo(_cts.Token);
        }

        private void ShowLog(string text)
        {
            _statusView.ShowLog(text).Forget();
            //_onShowLog.OnNext(text);
            //OnShowLog.Subscribe(killText => _statusView.ShowLog(killText).Forget()).AddTo(_cts.Token);
        }

        private void DestroyInGameUI()
        {
            _statusView.DestroyInGameUI();
            //_inGameUIDestroyed.OnNext(Unit.Default);
            //InGameUIDestroyed.Subscribe(_ => _statusView.DestroyInGameUI()).AddTo(_cts.Token);
        }

        private void ChangeDescriptionUI(int value)
        {
            _statusView.ChangeExhibitDescriptionUI(value);
            //_onChangeDescriptionUI.OnNext(value);
            //OnChangeDescriptionUI.Subscribe(_statusView.ChangeExhibitDescriptionUI).AddTo(_cts.Token);
        }
        
        private void StartTimer(NetworkRunner runner)
        {
            _statusView.ShowGameStartTime(runner).Forget();
            //_onStartTimer.OnNext(runner);
            //OnStartTimer.Subscribe(networkRunner => _statusView.ShowGameStartTime(networkRunner).Forget()).AddTo(_cts.Token);
        }

        private void ShowOgreLamp(bool isShow)
        {
            //_onShowOgreUI.Value = isShow;
            _statusView.ShowOgreLamp(isShow);
            //OnShowOgreUI.Subscribe(_statusView.ShowOgreLamp).AddTo(_cts.Token);
        }

        private void ChangeTagNotice(int messageType)
        {
            _statusView._changeTagOverlayMessage.ChangeTagNotice(messageType);
            //_changeTagNoticeObserver.OnNext(messageType);
            //ChangeTagNoticeObserver.Subscribe(index=>_statusView._changeTagOverlayMessage.ChangeTagNotice(index)).AddTo(_cts.Token);
        }

        private void ChangeSliderValue(int value)
        {
            _statusView.ChangeHp(value);
            //_onChangeSliderValue.Value = value;
            //OnChangeSliderValue.Subscribe(_statusView.ChangeHp).AddTo(_cts.Token);
        }

        private void ChangeStaminaValue(float value)
        {
            _statusView.ChangeStamina(value);
            //_onChangeStaminaValue.Value = value;
            //OnChangeStaminaValue.Skip(1).Subscribe(_statusView.ChangeStamina).AddTo(_cts.Token);
        }
        
        private void ShowInteractUI(bool isShow, GameObject target = null)
        {
            //_isInteracting.OnNext((isShow, target));
            // IsInteracting
            //     .Subscribe(isInteracting => _statusView._interactUI?.SetActive(isInteracting.Item1, isInteracting.Item2))
            //     .AddTo(_cts.Token);
            _statusView._interactUI?.SetActive(isShow, target);
        }
        
        private void SetInteractProgress(float progress)
        {
            _onChangeInteractProgress.Value = progress;
            if (progress >= 1.0f)
            {
                _isInteracting.OnNext((false, null));
            }

            _statusView._interactUI?.SetInteractProgress(progress);
            // OnChangeInteractProgress.Subscribe(progress => _statusView._interactUI?.SetInteractProgress(progress))
            //     .AddTo(_cts.Token);
        }

        private void ShowStatusUpUI(float seconds, StatusUpType status)
        {
            //_onInteractStatusUpObject.OnNext((seconds,status));
            _statusView.ShowStatusUpUI(seconds, status).Forget();
            // OnInteractStatusUpObject.Subscribe(info => _statusView.ShowStatusUpUI(info.Item1, info.Item2))
            //     .AddTo(_cts.Token);
        }

        private void OnClickOptionButton(bool value)
        {
            _statusView.ShowOptionUI(value);
            //OptionButtonClicked.Subscribe(_statusView.ShowOptionUI).AddTo(_cts.Token);
        }
    }
}