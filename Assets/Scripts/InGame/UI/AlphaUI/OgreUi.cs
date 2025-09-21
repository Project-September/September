using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace InGame.UI.AlphaUI
{
    public class OgreUi : MonoBehaviour
    {
        [Header("Refs")] 
        [SerializeField] private Image _ogreImage;
        [SerializeField] private Image _ogreEffectImage;

        [Header("Big Effect (起動時1回)")]
        [SerializeField] private float _bigScale = 1.6f;

        [SerializeField] private float _bigInTime = 0.35f;
        [SerializeField] private float _bigHoldTime = 0.25f;
        [SerializeField] private float _bigOutTime = 0.35f;

        [Header("Small Pulse (周期待機)")] 
        [SerializeField] private float _pulseInterval = 10f;

        [SerializeField] private bool _imageHeartbeat = true;
        [SerializeField] private float _imgBeat1Scale = 1.10f;
        [SerializeField] private float _imgBeat1In = 0.10f;
        [SerializeField] private float _imgBeat1Out = 0.14f;

        [SerializeField] private float _beatGap = 0.06f;

        [SerializeField] private float _imgBeat2Scale = 1.06f;
        [SerializeField] private float _imgBeat2In = 0.09f;
        [SerializeField] private float _imgBeat2Out = 0.14f;

        [SerializeField] private float _fxBeatAlpha = 0.45f;
        [SerializeField] private float _fxBeatIn = 0.10f;
        [SerializeField] private float _fxBeatOut = 0.16f;
        [SerializeField] private float _endAlpha = 0.0f;

        [Header("Alpha/Color")] 
        [SerializeField] private float _maxAlpha = 0.9f;
        [SerializeField] private Color _effectColor = Color.white;

        private CancellationTokenSource _cts;
        private RectTransform _effectRt;
        private Tweener _runningTween;

        private void Awake()
        {
            _effectRt = _ogreEffectImage ? _ogreEffectImage.rectTransform : null;
        }

        private void OnEnable()
        {
            StopAll();
            _cts = new CancellationTokenSource();
            RunLoopAsync(_cts.Token).Forget();
        }

        private void OnDisable() => StopAll();
        private void OnDestroy() => StopAll();

        private void StopAll()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (_runningTween != null && _runningTween.IsActive())
                _runningTween.Kill();

            if (_ogreEffectImage)
            {
                var c = _ogreEffectImage.color;
                c.a = 0f;
                _ogreEffectImage.color = c;
            }

            if (_effectRt) _effectRt.localScale = Vector3.one;
            if (_ogreImage) _ogreImage.rectTransform.localScale = Vector3.one;
        }

        private async UniTaskVoid RunLoopAsync(CancellationToken token)
        {
            if (_ogreImage) _ogreImage.gameObject.SetActive(true);

            if (_ogreEffectImage && _effectRt)
            {
                var c = _effectColor;
                c.a = 0f;
                _ogreEffectImage.color = c;
                _effectRt.localScale = Vector3.one;
                _ogreEffectImage.gameObject.SetActive(true);
            }

            // 起動時ド派手ワンショット（_ogreImage も拡大→収束）
            await PlayBigEffect(token);

            // 周期パルス
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, _pulseInterval)), cancellationToken: token);
                await PlayPulse(token);
            }
        }

        private async UniTask PlayBigEffect(CancellationToken token)
        {
            if (!_ogreEffectImage || _effectRt == null) return;

            RectTransform ogreImageRt = _ogreImage ? _ogreImage.rectTransform : null;

            _effectRt.localScale = Vector3.one * 0.01f;
            if (ogreImageRt) ogreImageRt.localScale = Vector3.one * 0.01f;

            var c = _effectColor;
            c.a = 0f;
            _ogreEffectImage.color = c;

            var scaleTweenEffect = _effectRt.DOScale(_bigScale, _bigInTime).SetEase(Ease.OutBack, overshoot: 1.4f);
            var fadeIn = _ogreEffectImage.DOFade(_maxAlpha, _bigInTime);

            Tween scaleTweenImage = null;
            if (ogreImageRt)
                scaleTweenImage = ogreImageRt.DOScale(_bigScale, _bigInTime).SetEase(Ease.OutBack, overshoot: 1.4f);

            await UniTask.WhenAll(
                scaleTweenEffect.AsyncWaitForCompletion().AsUniTask(),
                fadeIn.AsyncWaitForCompletion().AsUniTask(),
                scaleTweenImage != null ? scaleTweenImage.AsyncWaitForCompletion().AsUniTask() : UniTask.CompletedTask
            );

            await UniTask.Delay(TimeSpan.FromSeconds(_bigHoldTime), cancellationToken: token);

            var backEffect = _effectRt.DOScale(1f, _bigOutTime).SetEase(Ease.InSine);
            var fadeOut = _ogreEffectImage.DOFade(0f, _bigOutTime);

            Tween backImage = null;
            if (ogreImageRt)
                backImage = ogreImageRt.DOScale(1f, _bigOutTime).SetEase(Ease.InSine);

            await UniTask.WhenAll(
                backEffect.AsyncWaitForCompletion().AsUniTask(),
                fadeOut.AsyncWaitForCompletion().AsUniTask(),
                backImage != null ? backImage.AsyncWaitForCompletion().AsUniTask() : UniTask.CompletedTask
            );
        }

        private async UniTask PlayPulse(CancellationToken token)
        {
            if (!_ogreEffectImage || _effectRt == null) return;

            _effectRt.localScale = Vector3.one;
            var c = _effectColor;
            c.a = 0f;
            _ogreEffectImage.color = c;

            Sequence fxSeq = DOTween.Sequence();
            fxSeq.Append(_ogreEffectImage.DOFade(Mathf.Min(_fxBeatAlpha, 0.8f), _fxBeatIn));
            fxSeq.Append(_ogreEffectImage.DOFade(_endAlpha, _fxBeatOut));
            fxSeq.AppendInterval(_beatGap);

            fxSeq.Append(_ogreEffectImage.DOFade(Mathf.Min(_fxBeatAlpha * 0.9f, 0.8f), _fxBeatIn));
            fxSeq.Append(_ogreEffectImage.DOFade(_endAlpha, _fxBeatOut));

            Sequence imgSeq = null;
            if (_imageHeartbeat && _ogreImage)
            {
                var rt = _ogreImage.rectTransform;
                rt.localScale = Vector3.one;

                imgSeq = DOTween.Sequence();

                imgSeq.Append(rt.DOScale(_imgBeat1Scale, _imgBeat1In).SetEase(Ease.OutQuad));
                imgSeq.Append(rt.DOScale(1f, _imgBeat1Out).SetEase(Ease.InQuad));
                imgSeq.AppendInterval(_beatGap);

                imgSeq.Append(rt.DOScale(_imgBeat2Scale, _imgBeat2In).SetEase(Ease.OutQuad));
                imgSeq.Append(rt.DOScale(1f, _imgBeat2Out).SetEase(Ease.InQuad));
            }

            await UniTask.WhenAll(
                fxSeq.Play().AsyncWaitForCompletion().AsUniTask(),
                imgSeq != null
                    ? imgSeq.Play().AsyncWaitForCompletion().AsUniTask()
                    : UniTask.CompletedTask
            );
        }

        public void ShowOgreLamp(bool isShow)
        {
            if (_ogreImage) _ogreImage.gameObject.SetActive(isShow);
            if (_ogreEffectImage) _ogreEffectImage.gameObject.SetActive(isShow);
            enabled = isShow;
        }

        [ContextMenu("Debug/Trigger Big Effect")]
        private void DebugTriggerBig() => PlayBigEffect(_cts?.Token ?? CancellationToken.None).Forget();

        [ContextMenu("Debug/Trigger Pulse")]
        private void DebugTriggerPulse() => PlayPulse(_cts?.Token ?? CancellationToken.None).Forget();
    }
}