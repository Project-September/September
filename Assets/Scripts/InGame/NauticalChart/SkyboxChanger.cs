using Fusion;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class SkyboxChanger : NetworkBehaviour
{
    [Header("Prefab参照")]
    [SerializeField] private Material _defaultMaterial;
    [SerializeField] private Material _stormSkyboxMaterial;

    [Header("時間")]
    [SerializeField] private float _changeSkyTime = 10.0f;
    public float changeSkyTime => _changeSkyTime;

    /// <summary> Skyboxを変更する </summary>
    public async UniTaskVoid SkyBoxChange()
    {
        Material currentSkyboxMaterial = RenderSettings.skybox;
        RenderSettings.skybox = _stormSkyboxMaterial;
        await UniTask.WaitForSeconds(_changeSkyTime);
        RenderSettings.skybox = currentSkyboxMaterial;
    }
}
