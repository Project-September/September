using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary> 具体的な霧の処理するクラス </summary>
public class ConcreteFogController : IFogController
{
    [Header("Prefab参照")]
    [SerializeField] private GameObject[] _fogPrefab;
    [SerializeField] private ThunderFactory _thunderFactory;
    [SerializeField] private GameObject _canvas;

    [Header("霧の設定")]
    [Tooltip("FadeInからFadeOutまでの霧の持続時間"), SerializeField] private float _fogAnimInterval = 2.2f;
    [Tooltip("霧のフェードイン時間"), SerializeField] private float _fogFadeInTime = 0.4f;
    [Tooltip("霧のフェードアウト時間"), SerializeField] private float _fogFadeOutTime = 0.4f;
    [Tooltip("次の霧が表示されるまでの遅延時間"), SerializeField] private float _nextFogInterval = 0.08f;
    [Tooltip("霧のY座標FadeIn位置"), SerializeField] private float _fogFadeInY = 1.0f;
    [Tooltip("霧のY座標FadeOut位置"), SerializeField] private float _fogFadeOutY = -2.5f;

    CancellationTokenSource _cts = new CancellationTokenSource();

    private List<GameObject> _fogInstances = new List<GameObject>();

    /// <summary> 霧のFadeInアニメーション </summary>
    private async UniTaskVoid FogFadeIn()
    {
        for (int i = 0; i < _fogPrefab.Length; i++)
        {
            _fogInstances.Add(Object.Instantiate(_fogPrefab[i], _canvas.transform));
            _fogInstances[i].GetComponent<Image>().DOFade(1, _fogFadeInTime);
            _fogInstances[i].GetComponent<RectTransform>().DOAnchorPosY(_fogFadeInY, _fogFadeInTime);
            await UniTask.WaitForSeconds(_fogFadeInTime, cancellationToken: _cts.Token);
        }
    }

    /// <summary> 霧のFadeOutアニメーション </summary>
    private async UniTaskVoid FogFadeOut()
    {
       for (int i = 0; i < _fogInstances.Count; i++)
        {
            _fogInstances[i].GetComponent<Image>().DOFade(0, _fogFadeOutTime);
            _fogInstances[i].GetComponent<RectTransform>().DOAnchorPosY(0, _fogFadeOutTime);
            await UniTask.WaitForSeconds(_fogFadeOutTime, cancellationToken: _cts.Token);
        }
    }

    private async UniTaskVoid PlayFogFadeIn()
    {
        FogFadeIn().Forget();
        await UniTask.WaitForSeconds(_fogAnimInterval, cancellationToken: _cts.Token);
    }

    public void ShowFog()
    {
        PlayFogFadeIn().Forget();
        _thunderFactory.ThunderSpawener();
    }

    public void HideFog()
    {
        FogFadeOut().Forget();
    }

    ~ConcreteFogController()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}