using UnityEngine;
using InGame.Exhibit;

/// <summary> 具体的な霧の処理するクラス </summary>
public class ConcreteFogController : IFogController
{
    [SerializeField] private Material _stormSkyboxMaterial;

    /// <summary> Skyboxを変更する </summary>
    public void SkyBoxChange()
    {
        RenderSettings.skybox = _stormSkyboxMaterial;
    }


    public void ShowFog()
    {
        SkyBoxChange();
        // TODO：霧のエフェクトを実装する
    }

    public void HideFog()
    {
        // TODO：霧の効果を消す処理を実装する
    }
}