using Cinemachine;
using Fusion;
using UnityEngine;

public class AimCameraController : NetworkBehaviour
{
    [Header("通常のカメラ")]
    [SerializeField] private CinemachineVirtualCamera _normalCamera;
    [Header("AIM用のカメラ")]
    [SerializeField] private CinemachineVirtualCamera _aimCamera;
    
    [Header("CrosshairPrefab(照準のUI)")]
    [SerializeField] private GameObject _crosshairPrefab;
    private GameObject _crosshair;
    
    public Camera MainCamera { get; private set; }

    /// <summary>
    /// true：構えている状態　false：構えていない状態
    /// </summary>
    public bool IsAim { get; private set; }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            MainCamera = Camera.main;
            _crosshair = Instantiate(_crosshairPrefab);
            _crosshair.SetActive(false);
        }
    }

    /// <summary>
    /// 通常カメラに変更する
    /// ハタノAbilityが終了、AIM入力が辞めたとき
    /// </summary>
    public void NormalCamera()
    {
        IsAim = false;
        _normalCamera.gameObject.SetActive(true);
        _aimCamera.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// AIMカメラに変更する
    /// ハタノAbilityを発動したとき
    /// </summary>
    public void AimCamera()
    {
        if(!HasInputAuthority) return;
        
        IsAim = true;
        _normalCamera.gameObject.SetActive(false);
        _aimCamera.gameObject.SetActive(true);
        
        //プレイヤーの向きをカメラの方向に合わせる
        if (MainCamera == null)
        {
            MainCamera = Camera.main;
            Debug.LogWarning("MainCameraがnull");
            return;
        }
        var camForward = MainCamera.transform.forward;
        camForward.y = 0;
        gameObject.transform.forward = camForward;
    }

    /// <summary>
    /// 照準の表示を行う
    /// <param name="isFlag">true：表示　false：非表示</param>>
    /// </summary>
    public void CrosshairToggleChange(bool isFlag)
    {
        if(!HasInputAuthority) return;
        if(_crosshair == null)
        {
            Debug.LogWarning("Crosshairが生成されてない");
            return;
        }
        _crosshair.SetActive(isFlag);
    }

    /// <summary>
    /// カメラの方向にプレイヤーを向かせる
    /// </summary>
    public void PlayerDirectionCamera()
    {
        if (MainCamera == null)
        {
            MainCamera = Camera.main;
            Debug.LogWarning("MainCameraが設定されてない");
            return;
        }
        var camForward = MainCamera.transform.forward;
        camForward.y = 0;
        gameObject.transform.forward = camForward;
    }
}
