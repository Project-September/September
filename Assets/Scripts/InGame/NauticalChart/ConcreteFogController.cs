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
    [Header("Prefab参照")]
    [SerializeField] private Material _stormSkyboxMaterial;
    [SerializeField] private GameObject[] _fogPrefab;
    [SerializeField] private GameObject _thunderPrefab;

    [Header("霧の設定")]
    [Tooltip("霧のアニメーション開始までの遅延時間")]
    [SerializeField] private float _fogAnimInterval = 2.0f;
    [Tooltip("霧のフェードアウト時間")]
    [SerializeField] private float _fogFadeOutTime = 1.0f;
    [Tooltip("霧のY座標移動")]
    [SerializeField] private float _fogMoveY = -3.0f;
    [Tooltip("霧が消滅してから次の霧が表示されるまでの遅延時間")]
    [SerializeField] private float _nextFogInterval = 1.0f;

    [Header("雷の設定")]
    [SerializeField] private float _thunderLifeTime = 5.0f;
    [Tooltip("落下範囲")]
    [SerializeField] private float _minRadius; // 雷の落下範囲の最小値(船の範囲より大きい)
    [SerializeField] private float _maxRadius; // 雷の落下範囲の最大値

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
            GameObject fogInstance = Instantiate(_fogPrefab[i]);
            fogInstance.GetComponent<Image>().DOFade(0, _fogFadeOutTime).SetDelay(_fogAnimInterval);
            fogInstance.transform.DOMoveY(_fogMoveY, _fogFadeOutTime).SetDelay(_fogAnimInterval);
            Destroy(fogInstance, _fogAnimInterval + _fogFadeOutTime);
        }
    }

    /// <summary> 霧のエフェクトを生成する </summary>
    private async UniTaskVoid PlayFogAsync()
    {
        FogAnim();
        await UniTask.WaitForSeconds(_nextFogInterval, cancellationToken: _cts.Token);
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