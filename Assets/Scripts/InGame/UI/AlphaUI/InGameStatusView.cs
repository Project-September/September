using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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

        [Header("Canvas")] [SerializeField, Label("MainCanvas")]
        private Canvas _mainCanvas;

        [Header("Timer Settings")] [SerializeField, Label("TimerData")]
        private GameTimerData _timerData;

        private InGameUIRootRefs _uiRoot;
        private Slider _hpBarSlider;
        private Slider _staminaBarSlider;
        private TextMeshProUGUI _killLogText;
        private TextMeshProUGUI _ogreMessageText;
        private GameObject _optionUI;
        private GameObject _killLogUI;
        private GameObject _ogreUiInstance;
        private InteractUi _interactUI;
        private UniTask _ogreMessageTask;

        private CancellationTokenSource _cts;
        
        

        private void Awake()
        {
            _cts = new CancellationTokenSource();
            Bind();
        }

        private void Bind()
        {
            UIController ui = UIController.I;
            ui.OnGameStart.Subscribe(_ => SetupUI()).AddTo(_cts.Token);
            ui.OnChangeSliderValue.Subscribe(ChangeHp).AddTo(_cts.Token);
            ui.OnClickOptionButton.Subscribe(ShowOptionUI).AddTo(_cts.Token);
            ui.OnStartTimer.Subscribe(_ => ShowGameStartTime().Forget()).AddTo(_cts.Token);
            ui.OnNoticeKillLog.Subscribe(killText => ShowKillLog(killText).Forget()).AddTo(_cts.Token);
            ui.OnShowOgreUI.Subscribe(ShowOgreLamp).AddTo(_cts.Token);
            ui.OnChangeStaminaValue.Skip(1).Subscribe(ChangeStamina).AddTo(_cts.Token);
            ui.OnGameEnd.Subscribe(_ => PlayResultAnimation().Forget()).AddTo(_cts.Token);
            ui.IsInteracting
                .Subscribe(isInteracting => _interactUI?.SetActive(isInteracting.Item1, isInteracting.Item2))
                .AddTo(_cts.Token);
            ui.OnChangeInteractProgress.Subscribe(progress => _interactUI?.SetInteractProgress(progress))
                .AddTo(_cts.Token);
        }

        private void SetupUI()
        {
            if (!_uiRoot)
                _uiRoot = Instantiate(_inGameUiRootPrefab, _mainCanvas.transform);

            _optionUI = _uiRoot.OptionUI;
            _killLogUI = _uiRoot.KillLogPanel;
            _killLogText = _uiRoot.KillLogText;
            _ogreUiInstance = _uiRoot.OgreUI;
            _ogreMessageText = _uiRoot.OgreMessageText;
            _hpBarSlider = _uiRoot.HpBar;
            _staminaBarSlider = _uiRoot.StaminaBar;
            _interactUI = _uiRoot.InteractUI;
            _optionUI.SetActive(true);
            _killLogUI.SetActive(true);
            _ogreUiInstance.SetActive(false);
            _hpBarSlider.gameObject.SetActive(true);
            _staminaBarSlider.gameObject.SetActive(true);
            _interactUI.SetActive(false);
        }

        private void ChangeHp(int value)
        {
            if (_hpBarSlider == null)
                return;

            DOTween.To(() => _hpBarSlider.value, x => _hpBarSlider.value = x, value, 0.3f)
                .SetEase(Ease.OutQuad);
        }

        private async UniTask PlayResultAnimation()
        {
            var resultUI = Instantiate(_resultUIRootPrefab, _mainCanvas.transform);
            ResultAnimation resultAnim = resultUI.GetComponent<ResultAnimation>();
            await resultAnim.Play(resultUI);
        }

        private void ChangeStamina(float value)
        {
            if (_staminaBarSlider == null) 
                return;
            
            _staminaBarSlider.value = value;
        }

        // キルのログを直接引数に入れる
        private async UniTask ShowKillLog(string killText)
        {
            if (_killLogText == null)
                return;

            _killLogText.text = killText;
            _killLogUI.SetActive(true);

            RectTransform rect = _killLogText.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = _killLogText.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = _killLogText.gameObject.AddComponent<CanvasGroup>();

            // 初期状態（下側、完全表示）
            rect.anchoredPosition = new Vector2(0, 0);
            canvasGroup.alpha = 1f;

            float moveDistance = 100f;
            float duration = 2f;
            float fadeDuration = 0.5f;

            // DoTweenアニメーションを開始
            Sequence seq = DOTween.Sequence();
            seq.Append(rect.DOAnchorPosY(moveDistance, duration).SetEase(Ease.OutCubic));
            seq.Join(canvasGroup.DOFade(0f, fadeDuration).SetDelay(duration - fadeDuration));
            // DoTweenが完了するまで待機
            await seq.AsyncWaitForCompletion();

            // 完了後に削除 or 非表示
            _killLogText.gameObject.SetActive(false);
        }

        // ToDo : タイマークラスを作成してアニメーションなどを柔軟に行えるようにする
        private async UniTask ShowGameStartTime()
        {
            if (_uiRoot == null || _uiRoot.TimerText == null) 
                return;
            
            TextMeshProUGUI timer = _uiRoot.TimerText;
            timer.gameObject.SetActive(true);

            // カウントダウン
            for (int i = _timerData.PreStartTime; i >= 1; i--)
            {
                timer.text = i.ToString();
                await UniTask.Delay(TimeSpan.FromSeconds(_timerData.Duration), cancellationToken: _cts.Token);
            }
            
            timer.text = "Ready";
            await UniTask.Delay(TimeSpan.FromSeconds(_timerData.AfterReadyDelay), cancellationToken: _cts.Token);

            timer.text = "Go!";
            await UniTask.Delay(TimeSpan.FromSeconds(_timerData.Duration), cancellationToken: _cts.Token);

            for (float remaining = _timerData.GameTime; remaining >= 0; remaining--)
            {
                timer.text = TimeSpan.FromSeconds(remaining).ToString(@"mm\:ss");
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: _cts.Token);
            }

            timer.text = "Time Up!";
            await UniTask.Delay(TimeSpan.FromSeconds(_timerData.Duration), cancellationToken: _cts.Token);
            Destroy(timer.gameObject);
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
            if (_optionUI != null)
                _optionUI.SetActive(isShow);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}