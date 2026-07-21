using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.Pool;
using System.Collections.Generic;

/// <summary> 具体的な霧の処理するクラス </summary>
public class ConcreteFogController : MonoBehaviour, IFogController
{
    [Header("嵐Prefab")]
    [SerializeField] private Material _stormSkyboxMaterial;

    [Header("霧Prefab")]
    [SerializeField] private Image[] _fogPrefab;

    [Header("雷Prefab")]
    [SerializeField] private GameObject _thunderPrefab;

    [Header("霧のアニメーション設定")]
    [SerializeField] private float _fogMoveY = -3.0f;
    [SerializeField] private float _fogDelayAnimTime = 2.0f; // 次の霧のアニメーション開始までの遅延時間
    [SerializeField] private float _fogAnimTime = 1.0f;　// 霧のアニメーション時間
    [SerializeField] private float _fogLifeTime = 3.0f;　// 霧の寿命時間
    [SerializeField] private float _fogDelayLifeTime = 1.0f; // 次の霧が消滅するまでの遅延時間

    [Header("雷の設定")]
    [SerializeField] private float _thunderLifeTime = 5.0f;
    [SerializeField] private float _minRadius;
    [SerializeField] private float _maxRadius;

    CancellationTokenSource _cts;

    private void Start()
    {
        _cts = new CancellationTokenSource();
    }


    /// <summary> Skyboxを変更する </summary>
    public void SkyBoxChange()
    {
        RenderSettings.skybox = _stormSkyboxMaterial;
    }

    /// <summary> 雷エフェクトを生成する </summary>
    private void SpawnThunder()
    {
        GameObject thunderInstance = Instantiate(_thunderPrefab);

        //thunderInstance.transform.position.x = Random.Range(-_maxRadius, _maxRadius);
        //thunderInstance.transform.position.z = Random.Range(-_maxRadius, _maxRadius);

        Destroy(thunderInstance, _thunderLifeTime);
    }

    /// <summary> 霧のアニメーションを実行する </summary>
    private void FogAnim()
    {
        for (int i = 0; i < _fogPrefab.Length; i++)
        {
            Image fogInstance = Instantiate(_fogPrefab[i]);
            _fogPrefab[i].DOFade(0, _fogAnimTime).SetDelay(_fogDelayAnimTime);
            _fogPrefab[i].transform.DOMoveY(_fogMoveY, _fogAnimTime).SetDelay(_fogDelayAnimTime);
            Destroy(fogInstance, _fogLifeTime);
        }
    }

    /// <summary> 霧のエフェクトを生成する </summary>
    private async UniTaskVoid PlayFogAsync()
    {
        FogAnim();
        await UniTask.WaitForSeconds(_fogDelayLifeTime, cancellationToken: _cts.Token);
    }

    public void ShowFog()
    {
        SkyBoxChange();
        SpawnThunder();
        PlayFogAsync().Forget();
    }

    public void HideFog()
    {
        // TODO：霧の効果を消す処理を実装する
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _minRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _maxRadius);
    }
}