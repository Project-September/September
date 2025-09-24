using CRISound;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

public class TimeOverlayMessage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    //  OutElastic良い感じ
    [SerializeField] private Ease _ease;
    [SerializeField] private float _fadeOutDuration = 2.5f;
    [SerializeField, Label("準備時間開始テキスト")] private string _readyTimeMessage = "戦いに備えろ！";
    [SerializeField, Label("ゲーム開始テキスト")] private string _gameStartMessage = "ゲーム開始！";
    [SerializeField, Label("制限時間寸前テキスト")] private string _timeRemainingMessage = "残り30秒！！";
    private UniTask _currentTask;
    private void Awake()
    {
        _text.text = "";
        _text.transform.localScale = Vector3.zero;
    }

    public UniTask CallTask(TimeMessageType type)
    {
        if (!_currentTask.Status.IsCompleted()) return default;
        switch (type)
        {
            case TimeMessageType.Countdown:
                _currentTask = CountdownTask().Preserve();
                break;
            case TimeMessageType.GameStart:
                _currentTask = GameStartTask().Preserve();
                break;
            case TimeMessageType.TimeRemainingAlert:
                _currentTask = TimeRemainingAlertTask().Preserve();
                break;
        }
        return _currentTask;
    }
    private async UniTask CountdownTask()
    {
        _text.transform.localScale = Vector3.zero;
        var col = _text.color;
        col.a = 1;
        _text.color = col;
        _text.text = "3";
        CRIAudio.PlaySE("ALLCue", SoundCues.SE.UI_CountDown_Count.Name); // カウントダウン音
        await _text.transform.DOScale(Vector3.one, 0.5f).SetLoops(2,LoopType.Yoyo).SetEase(_ease);
        _text.text = "2";
        CRIAudio.PlaySE("ALLCue", SoundCues.SE.UI_CountDown_Count.Name); // カウントダウン音
        await _text.transform.DOScale(Vector3.one, 0.5f).SetLoops(2,LoopType.Yoyo).SetEase(_ease);
        _text.text = "1";
        CRIAudio.PlaySE("ALLCue", SoundCues.SE.UI_CountDown_Count.Name); // カウントダウン音
        await _text.transform.DOScale(Vector3.one, 0.5f).SetLoops(2,LoopType.Yoyo).SetEase(_ease);
        CRIAudio.PlaySE("ALLCue", SoundCues.SE.UI_CountDown_End.Name); // ゲーム開始音
        _text.text = _readyTimeMessage;
        await _text.transform.DOScale(Vector3.one, 0.5f).SetEase(_ease);
        await _text.DOFade(0f, _fadeOutDuration).SetEase(Ease.Linear);
    }

    private async UniTask GameStartTask()
    {
        _text.transform.localScale = Vector3.zero;
        var col = _text.color;
        col.a = 1;
        _text.color = col;
        _text.text = _gameStartMessage;
        await _text.transform.DOScale(Vector3.one, 0.5f).SetEase(_ease);
        await _text.DOFade(0f, _fadeOutDuration).SetEase(Ease.Linear);
    }

    private async UniTask TimeRemainingAlertTask()
    {
        _text.transform.localScale = Vector3.zero;
        var col = _text.color;
        col.a = 1;
        _text.color = col;
        _text.text = _timeRemainingMessage;
        await _text.transform.DOScale(Vector3.one, 0.5f).SetEase(_ease);
        await _text.DOFade(0f, _fadeOutDuration).SetEase(Ease.Linear);
    }
}

public enum TimeMessageType
{
    Countdown,
    GameStart,
    TimeRemainingAlert,
}