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
        [Header("アタッチが必要")] 
        [SerializeField] 
        private PreparationState _preparationState;
        [SerializeField] 
        private PlayingState _playingState;
        [SerializeField] 
        private EndingState _endingState;
        [SerializeField] 
        private MoaiInteractInvoker _moaiObj;
        [SerializeField] 
        private TutankhamenInteractRPCInvoker _tutankhamObj;
        [SerializeField]
        private PropAirplane _propAirplane;

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

        public InGameUIRootRefs UIRootRefs { get; set; }
        private InGameStatusView _statusView;
        private CancellationTokenSource _cts;

        private void Start()
        {
            InGameManager manager = StaticServiceLocator.Instance.Get<InGameManager>();
            StaticServiceLocator.Instance.Register(this);
            _cts = new CancellationTokenSource();
            manager.GameEnded += DestroyInGameUI;
            _preparationState.TimeOverlayMessage += _statusView.TimeOverlayMessage;
            _playingState.TimeOverlayMessage += _statusView.TimeOverlayMessage;
        }

        // イベントに登録しにいく
        private void SetUp()
        {
            // ゲーム開始時のUIを表示
            _preparationState.OnSetUI.Subscribe(_ => _statusView.SetupUI()).AddTo(_cts.Token);
            // 説明文の変更
            _preparationState.OnChangeDescriptionUI.Subscribe(_statusView.ChangeExhibitDescriptionUI).AddTo(_cts.Token);
            _propAirplane.OnChangeDescriptionUI.Subscribe(_statusView.ChangeExhibitDescriptionUI).AddTo(_cts.Token);
            // Timerの変更
            _preparationState.OnStartTimer.Subscribe(runner => _statusView.ShowGameStartTime(runner).Forget())
                .AddTo(_cts.Token);
            // 鬼UIの変更
            _preparationState.OnShowOgreUI.Subscribe(value => _statusView.ShowOgreLamp(value)).AddTo(_cts.Token);
            // ログの更新
            _preparationState.OnShowLog.Subscribe(value => _statusView.ShowLog(value).Forget()).AddTo(_cts.Token);
            // アイテムを使用したときにステータスが更新された時に表示するUIの変化
            _preparationState.OnInteractStatusUpObject
                .Subscribe(value => _statusView.ShowStatusUpUI(value.Item1, value.Item2).Forget()).AddTo(_cts.Token);
            _moaiObj.OnInteractStatusUpObject
                .Subscribe(value => _statusView.ShowStatusUpUI(value.Item1, value.Item2).Forget()).AddTo(_cts.Token);
            _tutankhamObj.OnInteractStatusUpObject
                .Subscribe(value => _statusView.ShowStatusUpUI(value.Item1, value.Item2).Forget()).AddTo(_cts.Token);
            // スコアの更新
            _playingState.OnChangeScore.Subscribe(value => _statusView.ChangeScore(value)).AddTo(_cts.Token);
            // Animationの更新
            _endingState.StartAnimation.Subscribe(_ => _statusView.PlayResultAnimation().Forget()).AddTo(_cts.Token);
        }

        private void DestroyInGameUI()
        {
            _statusView.DestroyInGameUI();
        }

        private void ChangeTagNotice(int messageType)
        {
            _statusView._changeTagOverlayMessage.ChangeTagNotice(messageType);
        }

        private void ChangeSliderValue(int value)
        {
            _statusView.ChangeHp(value);
        }

        private void ChangeStaminaValue(float value)
        {
            _statusView.ChangeStamina(value);
        }

        private void ShowInteractUI(bool isShow, GameObject target = null)
        {
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
        }

        private void OnClickOptionButton(bool value)
        {
            _statusView.ShowOptionUI(value);
        }
    }
}