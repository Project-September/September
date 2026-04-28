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
    
    [Networked]public Vector3 AimOrigin { get; private set; }
    [Networked]public Vector3 AimDirection { get; private set; }

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
    
    public override void FixedUpdateNetwork()
    {
        if(!HasInputAuthority || MainCamera == null) return;
        if (IsAim)
        {
            var camForward = MainCamera.transform.forward;
            camForward.y = 0;
            transform.forward = camForward;
        }
        
        RPC_SetAim(MainCamera.transform.position, MainCamera.transform.forward);
    }

    /// <summary>
    /// カメラの位置等をクライアントから送信しホスト側で変更
    /// </summary>
    /// <param name="aimOrigin">カメラ場所</param>
    /// <param name="aimDirection">カメラのForward</param>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetAim(Vector3 aimOrigin, Vector3 aimDirection)
    {
        AimOrigin = aimOrigin;
        AimDirection = aimDirection;
    }

    /// <summary>
    /// 通常カメラに変更する
    /// ハタノAbilityが終了、AIM入力が辞めたとき
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_NormalCamera()
    {
        IsAim = false;
        _normalCamera.gameObject.SetActive(true);
        _aimCamera.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// AIMカメラに変更する
    /// ハタノAbilityを発動したとき
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AimCamera()
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
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CrosshairToggleChange(bool isFlag)
    {
        if(!HasInputAuthority) return;
        if(_crosshair == null)
        {
            Debug.LogWarning("Crosshairが生成されてない");
            return;
        }
        _crosshair.SetActive(isFlag);
    }
}
