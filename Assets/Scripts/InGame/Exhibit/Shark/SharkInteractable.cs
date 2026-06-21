using UnityEngine;
using Fusion;
using InGame.Exhibit;
using InGame.Interact;
using September.Common;

public class SharkInteractable : MountableExhibitBase
{
    [Header("歩く速度"), SerializeField]
    private float _walkSpeed;
    [Header("走る速度"), SerializeField]
    private float _dashSpeed;
    [Header("Rayの最大距離（地面検出用）"), SerializeField]
    private float _rayDistance;
    [Header("地面扱いになる最大角度"), SerializeField]
    private float _groundMaximumAngle;
    [Header("空中時の落下加速度"), SerializeField]
    private Vector3 _fallGravity;
    [Header("最大回転角度"), SerializeField] 
    private float _maxRotateValue;
    [Header("攻撃のクールダウンタイム"), SerializeField]
    private float _cooldownTime;
    
    /// <summary>
    /// インタラクション中か
    /// <para>true：インタラクション中　false：インタラクション中でない</para>
    /// </summary>
    [Networked] public bool IsSharkInteracting { get; private set; }
    
    private InteractableBase _interactable;
    
    private Vector3 _groundNormal; // 地面の角度
    private bool _isGround; // 地面についているか
    private float _cooldownTimer; // 攻撃のクールダウンタイマー
    
    // TODO：攻撃
    

    public override void Spawned()
    {
        base.Spawned();
        _interactable = GetComponent<InteractableBase>();
    }

    public override void GetOn(PlayerRef playerRef)
    {
        base.GetOn(playerRef);
        _interactable.ForceSetInteractable = false;
        _cooldownTimer = _cooldownTime;
    }

    public override void GetOff(PlayerRef playerRef)
    {
        base.GetOff(playerRef);
        _interactable.ForceSetInteractable = true;
    }

    public override void OnInteractFixedUpdate(PlayerInput playerInput, float deltaTime)
    {
        base.OnInteractFixedUpdate(playerInput, deltaTime);
        if(!HasStateAuthority) return;
        CheckGroundManual();
        var moveDirection = Move(playerInput);
        moveDirection.y = 0;　
        Rotate(deltaTime, moveDirection);　//回転
        AdsorptionOnGround(deltaTime);
    }
    
    /// <summary>
    /// Raycastで地面の法線を取得する
    /// </summary>
    private void CheckGroundManual()
    {
        bool ray = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, _rayDistance);
        var normal = hit.normal;
        if (ray && Vector3.Angle(normal, Vector3.up) < _groundMaximumAngle)
        {
            _isGround = true;
            _groundNormal = normal;
            return;
        }

        if (!ray || Vector3.Angle(normal, Vector3.up) >= _groundMaximumAngle)
        {
            // 下向きの加速度を加える
            _isGround = false;
            Rigidbody.AddForce(_fallGravity, ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// 鮫の移動処理
    /// </summary>
    /// <param name="playerInput">プレイヤーの入力</param>
    private Vector3 Move(PlayerInput playerInput)
    {
        var inputMoveDirection = playerInput.MoveDirection;
        if (playerInput.MoveDirection == Vector2.zero)
        {
            return Vector3.zero;
        }
        var moveVector2 = GetMoveDirection(inputMoveDirection, playerInput.CameraYaw);
        var moveDirection = new Vector3(moveVector2.x, 0, moveVector2.y);
        var moveVelocity = Vector3.ProjectOnPlane(moveDirection, _groundNormal).normalized;　//坂に沿った動きに
        if (playerInput.Buttons.IsSet(PlayerButtons.Dash))
        {
            Rigidbody.linearVelocity = moveVelocity * _dashSpeed;
        }
        else
        {
            Rigidbody.linearVelocity = moveVelocity * _walkSpeed;
        }
        return moveDirection;
    }
    
    /// <summary> カメラ視点の移動入力を取得 </summary>
    Vector2 GetMoveDirection(Vector2 moveInput, float cameraYaw)
    {
        float radYaw = -cameraYaw * Mathf.Deg2Rad;
        return new Vector2(
            moveInput.x * Mathf.Cos(radYaw) - moveInput.y * Mathf.Sin(radYaw),
            moveInput.x * Mathf.Sin(radYaw) + moveInput.y * Mathf.Cos(radYaw)
        );
    }
    
    /// <summary>
    /// 移動方向へ滑らかに回転をする
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <param name="moveDirection"></param>
    private void Rotate(float deltaTime, Vector3 moveDirection)
    {
        if (moveDirection == Vector3.zero) return;
        // -90の補正を掛けて、常に横向きにする ※仮オブジェクトのため、本来のモデルなら必要なくなる
        var rot = Quaternion.LookRotation(moveDirection) * Quaternion.Euler(-90, 0, 0);
        var endRot = Quaternion.RotateTowards(transform.rotation, rot, _maxRotateValue * deltaTime);
        transform.rotation = endRot;
    }

    private void AdsorptionOnGround(float deltaTime)
    {
        if (_isGround) return;
        var ray = Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit,
            _rayDistance);
        if (ray && hit.distance > 0)
        {
            var targetPos = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            Rigidbody.MovePosition(Vector3.Lerp(transform.position, targetPos, deltaTime * 10f));
        }
    }
}
