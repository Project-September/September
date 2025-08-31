using Fusion;
using InGame.Player;
using September.Common;
using UnityEngine;

/// <summary>
/// シンプルなMoverクラス。地面に沿って移動し、キャラを回転させる
/// </summary>
public class PlayerMovementV2 : NetworkTRSP
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] float _moveSpeed = 6f;   // 目標水平速度 [m/s]
    [SerializeField] float _accel     = 40f;  // 加速・減速 [m/s^2]
    [SerializeField] private LayerMask _groundLayer = ~0;
    [SerializeField, Tooltip("地面と認識する最大角度")] private float _groundSlopeThreshold = 45f;
    [SerializeField] private float _groundCheckDistance = 0.5f;
    [SerializeField] CameraController _cameraController;
    private PlayerInput _input;
    private float _isGroundTimer;
    private Vector3 _groundNormal = Vector3.up;
    
    public bool IsGround { get; set; }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            _cameraController = GetComponent<CameraController>();
            _cameraController.Init(this);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput<PlayerInput>(out var input)) return;
        //if (!Object.HasStateAuthority || !Object.HasInputAuthority) return; // 物理は通常State Authorityのみ

        Vector2 md = GetMoveDirection(input.MoveDirection, input.CameraYaw);
        if (md.sqrMagnitude > 1f) md.Normalize();
        if (md.sqrMagnitude < 0.0004f) md = Vector2.zero;

        Vector3 targetHoriz = new Vector3(md.x, 0f, md.y) * _moveSpeed;
        targetHoriz = AlignToGround(targetHoriz);

        float dt = Runner.DeltaTime;
        Vector3 v = _rb.linearVelocity;
        Vector3 curHoriz = new Vector3(v.x, 0f, v.z);
        Vector3 nextHoriz = Vector3.MoveTowards(curHoriz, targetHoriz, _accel * dt);

        v.x = nextHoriz.x;
        v.z = nextHoriz.z; // v.y は重力任せ
        _rb.linearVelocity = v;

        // 任意：移動方向へキャラを緩やかに回す
        if (nextHoriz.sqrMagnitude > 0.0001f)
        {
            Quaternion to = Quaternion.LookRotation(nextHoriz.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, to, 12f * dt);
        }
        
        CheckGroundManual();
    }
    
    private Vector3 AlignToGround(Vector3 v)
    {
        if (!IsGround) return v;
        return Vector3.ProjectOnPlane(v, _groundNormal);
    }

    private Vector2 GetMoveDirection(Vector2 moveInput, float cameraYaw)
    {
        float rad = -cameraYaw * Mathf.Deg2Rad;
        return new Vector2(
            moveInput.x * Mathf.Cos(rad) - moveInput.y * Mathf.Sin(rad),
            moveInput.x * Mathf.Sin(rad) + moveInput.y * Mathf.Cos(rad)
        );
    }
    
    public float GetSpeedOnPlane()
    {
        Quaternion normalRot = Quaternion.FromToRotation(_groundNormal, Vector3.up);
        Vector3 onPlaneVec = normalRot * _rb.linearVelocity;
        onPlaneVec.y = 0;
        return onPlaneVec.magnitude;
    }
    
    private void CheckGroundManual()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out var hitInfo, _groundCheckDistance, _groundLayer))
        {
            if (Vector3.Angle(Vector3.up, hitInfo.normal) <= _groundSlopeThreshold)
            {
                IsGround = true;
                _groundNormal = hitInfo.normal; 
            }
            else
            {
                IsGround = false;
                _groundNormal = Vector3.up;
            }
        }
        else
        {
            IsGround = false;
            _groundNormal = Vector3.up;
        }
    }
}
