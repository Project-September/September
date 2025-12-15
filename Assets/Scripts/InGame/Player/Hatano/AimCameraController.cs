using Cinemachine;
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
    private bool _isAim;
    
    void Start()
    {
        MainCamera = Camera.main;
    }

    /// <summary>
    /// カメラの切り替えを行う
    /// </summary>
    public void CameraToggleChange()
    {
        if (_isAim) //構えている状態
        {
            //カメラを通常に戻す
            _normalCamera.gameObject.SetActive(true);
            _aimCamera.gameObject.SetActive(false);
            
            _isAim = false;
        }
        else
        {
            //カメラをAIMにする
            _normalCamera.gameObject.SetActive(false);
            _aimCamera.gameObject.SetActive(true);
            
            _isAim = true;
        }
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
