using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusion;
using InGame.Exhibit;
using NaughtyAttributes;
using Result;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace September.InGame.UI
{
    /// <summary>UIの管理</summary>
    public class InGameStatusView : MonoBehaviour
    {
        [Header("UI Root Prefab")] 
        [SerializeField, Label("InGameUIRoot")] private InGameUIRootRefs _inGameUiRootPrefab;
        [SerializeField] private ResultUIRootRefs _resultUIRootPrefab;

        [Header("Canvas")] 
        [SerializeField, Label("MainCanvas")] private Canvas _mainCanvas;

        [Header("Timer Settings")] 
        [SerializeField, Label("TimerData")] private GameTimerData _timerData;
        
        [Header("キルログ")]
        [SerializeField] private GameObject  _killLogItemPrefab;
        [SerializeField] private int _maxLogCount = 5;
        
        [SerializeField] private CanvasGroup _statusUpUI;
        private VerticalLayoutGroup _statusUpLayout;

        private InGameUIRootRefs _uiRoot;
        private Slider _hpBarSlider;
        private Slider _staminaBarSlider;
        private readonly Queue<GameObject> _killLogQueue = new();
        private TextMeshProUGUI _ogreMessageText;
        private GameObject _optionUI;
        private GameObject _LogPanel;
        private GameObject _ogreUiInstance;
        private GameObject[] _descriptionIcon;
        private InteractUi _interactUI;
        private UniTask _ogreMessageTask;
        private float _tutanCounter;
        private TextMeshProUGUI _scoreText;
        private CancellationTokenSource _cts;
        private StatusUpType _currentStatusUpType;
        private CanvasGroup _ogreGroup;
        private void Awake()
        {
            _cts = new CancellationTokenSource();
            Bind();
            _tutanCounter = 0;
        }

        private void Bind()
        {
            UIController ui = UIController.I;
            ui.OnGameStart.Subscribe(_ => SetupUI()).AddTo(_cts.Token);
            ui.OnChangeSliderValue.Subscribe(ChangeHp).AddTo(_cts.Token);
            ui.OnClickOptionButton.Subscribe(ShowOptionUI).AddTo(_cts.Token);
            ui.OnStartTimer.Subscribe(runner => ShowGameStartTime(runner).Forget()).AddTo(_cts.Token);
            ui.OnShowLog.Subscribe(killText => ShowLog(killText).Forget()).AddTo(_cts.Token);
            ui.OnShowOgreUI.Subscribe(ShowOgreLamp).AddTo(_cts.Token);
            ui.OnChangeStaminaValue.Skip(1).Subscribe(ChangeStamina).AddTo(_cts.Token);
            ui.OnGameEnd.Subscribe(_ => PlayResultAnimation().Forget()).AddTo(_cts.Token);
            ui.IsInteracting
                .Subscribe(isInteracting => _interactUI?.SetActive(isInteracting.Item1, isInteracting.Item2))
                .AddTo(_cts.Token);
            ui.OnChangeInteractProgress.Subscribe(progress => _interactUI?.SetInteractProgress(progress))
                .AddTo(_cts.Token);
            ui.OnInteractStatusUpObject.Subscribe(info => ShowStatusUpUI(info.Item1, info.Item2))
                .AddTo(_cts.Token);
            ui.OnChangeDescriptionUI.Subscribe(ChangeExhibitDescriptionUI).AddTo(_cts.Token);
            ui.OnChangeScoreText.Subscribe(ChangeScore).AddTo(_cts.Token);
        }

        private void SetupUI()
        {
            if (!_uiRoot)
                _uiRoot = Instantiate(_inGameUiRootPrefab, _mainCanvas.transform);

            _optionUI = _uiRoot.OptionUI;
            _LogPanel = _uiRoot.LogPanel;
            _ogreUiInstance = _uiRoot.OgreUI;
            _ogreMessageText = _uiRoot.OgreMessageText;
            _descriptionIcon =  _uiRoot.DescriptionIcon;
            _hpBarSlider = _uiRoot.HpBar;
            _scoreText =  _uiRoot.ScoreText;
            _staminaBarSlider = _uiRoot.StaminaBar;
            _interactUI = _uiRoot.InteractUI;
            _statusUpUI = _uiRoot.StatusUpGroup;
            _statusUpLayout = _uiRoot.StatusUpUIRoot;
            _optionUI.SetActive(true);
            _LogPanel.SetActive(true);
            _ogreUiInstance.SetActive(false);
            _hpBarSlider.gameObject.SetActive(true);
            _staminaBarSlider.gameObject.SetActive(true);
            _interactUI.SetActive(false);
            _statusUpUI.gameObject.SetActive(true);
        }

        private void ChangeHp(int value)
        {
            if (!_hpBarSlider)
                return;

            DOTween.To(() => _hpBarSlider.value, x => _hpBarSlider.value = x, value, 0.3f)
                .SetEase(Ease.OutQuad);
        }

        private void ChangeScore(int value)
        {
            _scoreText.text = value.ToString();
        }

        private async UniTask PlayResultAnimation()
        {
            ResultUIRootRefs resultUI = Instantiate(_resultUIRootPrefab, _mainCanvas.transform);
            ResultAnimation resultAnim = resultUI.GetComponent<ResultAnimation>();
            await resultAnim.Play(resultUI);
        }

        public void ChangeExhibitDescriptionUI(bool value)
        {
            if (value)
            {
                // 展示物操作方法をアクティブ
                _descriptionIcon[0].SetActive(false);
                _descriptionIcon[1].SetActive(true);
            }
            else
            {
                // Player操作をアクティブ
                _descriptionIcon[0].SetActive(true);
                _descriptionIcon[1].SetActive(false);
            }
        }

        private void ChangeStamina(float value)
        {
            if (!_staminaBarSlider) 
                return;
            
            _staminaBarSlider.value = value;
        }

        // キルのログを直接引数に入れる
        private async UniTask ShowLog (string killText)
        {
            GameObject killLog = Instantiate(_killLogItemPrefab, _LogPanel.transform);

            TextMeshProUGUI tmp = killLog.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = killText;
            
            
            // フェード用 CanvasGroup
            CanvasGroup cg = killLog.GetComponent<CanvasGroup>() ?? killLog.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            await cg.DOFade(1f, 0.3f);

            _killLogQueue.Enqueue(killLog);
            
            if (_killLogQueue.Count > _maxLogCount)
            {
                GameObject old = _killLogQueue.Dequeue();
                if (old)
                {
                    CanvasGroup oldCg = old.GetComponent<CanvasGroup>() ?? old.AddComponent<CanvasGroup>();
                    oldCg.DOFade(0f, 0.5f).OnComplete(() => Destroy(old));
                }
            }
        }
        
        private async UniTask ShowGameStartTime(NetworkRunner runner)
        {
            if (!_uiRoot || !_uiRoot.TimerText) 
                return;
            
            TextMeshProUGUI timer = _uiRoot.TimerText;
            timer.gameObject.SetActive(true);
            
            // Tick基準
            int tickRate = runner.TickRate;
            
            // カウントダウン
            int preStartEndTick = runner.Tick + _timerData.PreStartTime * tickRate;
            while (runner.Tick < preStartEndTick)
            {
                int remaining = preStartEndTick - runner.Tick;
                timer.text = Mathf.CeilToInt(remaining / (float)tickRate).ToString();
                await UniTask.Yield(PlayerLoopTiming.Update,_cts.Token);
            }

            // ゲーム時間
            int gameEndTick = runner.Tick + (int)(_timerData.GameTime * tickRate);
            int lastTick = runner.Tick;
            while (runner.Tick < gameEndTick)
            {
                if (runner.Tick == lastTick)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, _cts.Token); 
                    continue;
                }
                
                int remaining = gameEndTick - runner.Tick;
                int seconds = Mathf.CeilToInt(remaining / (float)tickRate);
                timer.text = TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");
                await UniTask.Yield(PlayerLoopTiming.Update,_cts.Token);
            }

            timer.text = "Time Up!";
            await UniTask.Delay(TimeSpan.FromSeconds(_timerData.Duration), cancellationToken: _cts.Token);
        }
        
        // 鬼の時にUIを表示する
        private void ShowOgreLamp(bool isShow)
        {
            if (!_ogreUiInstance) return;
            _ogreUiInstance.gameObject.SetActive(isShow);
            if (isShow && _ogreMessageTask.Status.IsCompleted())
            {
                _ogreMessageTask = OgreMessage().Preserve();
            }
        }
        //  鬼になったことを伝えるメッセージ
        private async UniTask OgreMessage()
        {
            var col = _ogreMessageText.color;
            col.a = 0;
            _ogreMessageText.color = col;
            _ogreMessageText.text = "あなたが鬼です";
            await _ogreMessageText.DOFade(1, 1f);
            await UniTask.WaitForSeconds(2f);
            await _ogreMessageText.DOFade(0, 1f);
        }

        private void ShowOptionUI(bool isShow)
        {
            if (_optionUI)
                _optionUI.SetActive(isShow);
        }

        private async UniTaskVoid ShowStatusUpUI(float seconds,StatusUpType statusUpType)
        {
            switch (statusUpType)
            {
                case StatusUpType.Heal:
                {
                    var ui = Instantiate(_statusUpUI, _statusUpLayout.transform);
                    ui.GetComponentInChildren<TextMeshProUGUI>().text = "バイオリン：体力が回復した";
                    UpdateLayOutGroup();
                    await ui.DOFade(1, 0.5f);
                    await UniTask.Delay(TimeSpan.FromSeconds(seconds));
                    await ui.DOFade(0, 0.5f);
                    Destroy(ui.gameObject);
                    UpdateLayOutGroup();
                    break;
                }
                case StatusUpType.Tutankhamen:
                {
                    var ui = Instantiate(_statusUpUI, _statusUpLayout.transform);
                    ui.GetComponentInChildren<TextMeshProUGUI>().text = "ツタンカーメン：移動速度と攻撃力が上昇中";
                    UpdateLayOutGroup();
                    await ui.DOFade(1, 0.5f);
                    await UniTask.Delay(TimeSpan.FromSeconds(seconds - 1f));
                    await ui.DOFade(0, 0.5f);
                    Destroy(ui.gameObject);
                    UpdateLayOutGroup();
                    break;
                }
                case StatusUpType.Ogre:
                {
                    var ui = Instantiate(_statusUpUI, _statusUpLayout.transform);
                    ui.GetComponentInChildren<TextMeshProUGUI>().text = "鬼：移動速度と攻撃力が上昇中";
                    UpdateLayOutGroup();
                    _ogreGroup = ui;
                    await ui.DOFade(1, 0.5f);
                    break;
                }
                case StatusUpType.BokeBoke:
                {
                    var ui = Instantiate(_statusUpUI, _statusUpLayout.transform);
                    ui.GetComponentInChildren<TextMeshProUGUI>().text = "モアイ：スコアが増えた";
                    UpdateLayOutGroup();
                    await ui.DOFade(1, 0.5f);
                    await UniTask.Delay(TimeSpan.FromSeconds(seconds - 1f));
                    await ui.DOFade(0, 0.5f);
                    Destroy(ui.gameObject);
                    UpdateLayOutGroup();
                    break;
                }
                case StatusUpType.None:
                {
                    Destroy(_ogreGroup?.gameObject);
                    UpdateLayOutGroup();
                    break;
                }
                default:
                    break;
            }
        }

        private void UpdateLayOutGroup()
        {
            // レイアウト内の入力値を再計算
            _statusUpLayout.CalculateLayoutInputHorizontal();

            // レイアウト再設定(表示更新)
            _statusUpLayout.SetLayoutHorizontal();
        }
        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}