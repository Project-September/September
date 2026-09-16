using Fusion;
using InGame.Player;
using September.Common;
using Unity.Cinemachine;
using UnityEngine;

public class AimCameraController : NetworkBehaviour
{
    [Header("通常のカメラ"), SerializeField] private CinemachineVirtualCamera _normalCamera;
    [Header("AIM用のカメラ"), SerializeField] private CinemachineVirtualCamera _aimCamera;
    [Header("ULT用のカメラ"), SerializeField] private CinemachineVirtualCamera _ultCamera;
    [Header("CrosshairPrefab(照準のUI)")]
    [SerializeField] private GameObject _crosshairPrefab;
    [Header("回転のスムーズさ"), SerializeField] private float _rotationSpeed = 15f;
    private GameObject _crosshair;
    public Camera MainCamera { get; private set; }
    
    [Networked]public Vector3 AimOrigin { get; private set; }
    [Networked]public Vector3 AimDirection { get; private set; }

    /// <summary>
    /// true：構えている状態　false：構えていない状態
    /// </summary>
    [Networked] private NetworkBool IsAiming { get; set; }
    [Networked] private NetworkBool IsUltAiming { get; set; }
    public bool IsAim => IsAiming;
    public bool IsFacingCamera => IsAiming || IsUltAiming;

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
        if (!GetInput<PlayerInput>(out var input)) return;
        AimOrigin = input.CameraPosition;
        AimDirection = input.DesiredLookDirection;
    }

    /// <summary>
    /// 同期された構え状態を所有プレイヤーのカメラに適用する。
    /// </summary>
    public override void Render()
    {
        if (!HasInputAuthority) return;
        _normalCamera.gameObject.SetActive(!IsFacingCamera);
        _aimCamera.gameObject.SetActive(IsAim);
        _ultCamera.gameObject.SetActive(IsUltAiming);
    }

    /// <summary>Ends aiming and restores the normal camera/UI for every peer.</summary>
    public void StopAim()
    {
        if (!IsAim) return;

        RPC_NormalCamera();
        RPC_CrosshairToggleChange(false);
    }

    /// <summary>
    /// 通常カメラに変更する
    /// ハタノAbilityが終了、AIM入力が辞めたとき
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_NormalCamera()
    {
        if (!HasStateAuthority) return;
        IsAiming = false;
        IsUltAiming = false;
    }
    
    /// <summary>
    /// AIMカメラに変更する
    /// ハタノAbilityを発動したとき
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_AimCamera()
    {
        if (!HasStateAuthority) return;
        IsAiming = true;
        IsUltAiming = false;
    }

    /// <summary>
    /// ULTカメラに変更する
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ULTCamera()
    {
        if (!HasStateAuthority) return;
        IsAiming = false;
        IsUltAiming = true;
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
