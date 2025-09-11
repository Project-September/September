using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class CountdownAnimation : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    //  OutElastic良い感じ
    [SerializeField] private Ease _ease;
    [SerializeField] private float _fadeOutDuration = 2.5f;
    private UniTask _currentTask;
    private void Awake()
    {
        _text.text = "";
        _text.transform.localScale = Vector3.zero;
    }

    public void StartCountdown()
    {
        if (!_currentTask.Status.IsCompleted()) return;
        _currentTask = Countdown().Preserve();;
    }
    private async UniTask Countdown()
    {
        _text.transform.localScale = Vector3.zero;
        _text.color = Color.white;
        _text.text = "3";
        await _text.transform.DOScale(Vector3.one, 0.5f).SetLoops(2,LoopType.Yoyo).SetEase(_ease);
        _text.text = "2";
        await _text.transform.DOScale(Vector3.one, 0.5f).SetLoops(2,LoopType.Yoyo).SetEase(_ease);
        _text.text = "1";
        await _text.transform.DOScale(Vector3.one, 0.5f).SetLoops(2,LoopType.Yoyo).SetEase(_ease);
        _text.text = "ReadyTime!";
        await _text.transform.DOScale(Vector3.one, 0.5f).SetEase(_ease);
        _text.DOColor(Color.clear, _fadeOutDuration).SetEase(Ease.Linear);
    }
}
