using Cinemachine;
using Fusion;
using UnityEngine;

public class AimCameraController : MonoBehaviour
{
    [Header("通常のカメラ")]
    [SerializeField] private CinemachineVirtualCamera _normalCamera;
    [Header("AIM用のカメラ")]
    [SerializeField] private CinemachineVirtualCamera _aimCamera;
    
    [Header("CrosshairPrefab(照準のUI)")]
    [SerializeField] private GameObject _crosshairPrefab;
    private GameObject _crosshair;
    private bool _isCrosshairGeneration; //照準のUIを生成済みか
    
    public Camera MainCamera { get; private set; }

    /// <summary>
    /// true：構えている状態　false：構えていない状態
    /// </summary>
    public bool IsAim { get; private set; }

    void Start()
    {
        MainCamera = Camera.main;
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
        IsAim = true;
        _normalCamera.gameObject.SetActive(false);
        _aimCamera.gameObject.SetActive(true);
        
        //プレイヤーの向きをカメラの方向に合わせる
        this.gameObject.transform.forward = MainCamera.transform.forward;
    }

    /// <summary>
    /// 照準の表示を行う
    /// <param name="isFlag">true：表示　false：非表示</param>>
    /// </summary>
    public void CrosshairToggleChange(bool isFlag)
    {
        if (!_isCrosshairGeneration) //照準を生成していない場合、照準を表示する
        {
            _crosshair = Instantiate(_crosshairPrefab);
            _crosshair.SetActive(true);
            _isCrosshairGeneration = true;
        }
        else //照準を生成している場合、照準の表示、非表示を行う
        {
            _crosshair.SetActive(isFlag);
        }
    }
}
