using Fusion;
using September.Common;
using UnityEngine;

/// <summary>
/// サメの移動処理
/// </summary>
public class SharkMovementProcessing : NetworkBehaviour
{
    [Header("移動設定"), SerializeField] private float _walkSpeed;
    [SerializeField] private float _dashSpeed;
    [SerializeField] private float _rayDistance;
    [SerializeField] private float _groundMaximumAngle;
    [SerializeField] private Vector3 _fallGravity;
    [SerializeField] private float _maxRotateValue;

    /// <summary>
    /// 海に落ちる直前の位置
    /// </summary>
    public Vector3 PositionBeforeWaterFall { get; private set; }

    private Vector3 _currentGroundNormal; // 現在、接触している地面の法線
    private bool _isGrounded; // プレイヤーが地面に接地しているか

    /// <summary>
    /// Raycastで地面の法線を取得する
    /// </summary>
    /// <param name="rb">プレイヤーのRigidbody</param>
    public void CheckGroundManual(Rigidbody rb)
    {
        bool ray = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit,
            _rayDistance);
        var normal = hit.normal;
        if (ray && Vector3.Angle(normal, Vector3.up) < _groundMaximumAngle)
        {
            _isGrounded = true;
            _currentGroundNormal = normal;
            return;
        }

        if (!ray || Vector3.Angle(normal, Vector3.up) >= _groundMaximumAngle)
        {
            // 下向きの加速度を加える
            _isGrounded = false;
            rb.AddForce(_fallGravity, ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// サメの移動処理
    /// </summary>
    /// <param name="playerInput">プレイヤーの入力</param>
    /// <param name="rb">プレイヤーのRigidbody</param>
    public Vector3 Move(PlayerInput playerInput, Rigidbody rb)
    {
        var inputMoveDirection = playerInput.MoveDirection;
        if (playerInput.MoveDirection == Vector2.zero)
        {
            return Vector3.zero;
        }

        //　カメラを考慮した移動方向を取得
        var moveVector2 = GetMoveDirection(inputMoveDirection, playerInput.CameraYaw);
        var moveDirection = new Vector3(moveVector2.x, 0, moveVector2.y);
        var moveVelocity = Vector3.ProjectOnPlane(moveDirection, _currentGroundNormal).normalized;　//坂に沿った動きに
        if (playerInput.Buttons.IsSet(PlayerButtons.Dash))
        {
            rb.linearVelocity = moveVelocity * _dashSpeed;
        }
        else
        {
            rb.linearVelocity = moveVelocity * _walkSpeed;
        }

        return moveDirection;
    }

    /// <summary>
    /// カメラ視点の移動入力を取得
    /// </summary>
    private Vector2 GetMoveDirection(Vector2 moveInput, float cameraYaw)
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
    /// <param name="moveDirection">プレイヤーの移動方向</param>
    public void Rotate(float deltaTime, Vector3 moveDirection)
    {
        if (moveDirection == Vector3.zero) return;
        // -90の補正を掛けて、常に横向きにする ※仮オブジェクトのため、本来のモデルなら必要なくなる
        var rot = Quaternion.LookRotation(moveDirection) * Quaternion.Euler(-90, 0, 0);
        var endRot = Quaternion.RotateTowards(transform.rotation, rot, _maxRotateValue * deltaTime);
        transform.rotation = endRot;
    }

    /// <summary>
    /// 地面へと吸着
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <param name="rb">プレイヤーのRigidbody</param>
    public void AdsorptionOnGround(float deltaTime, Rigidbody rb)
    {
        if (_isGrounded) return;
        var ray = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit,
            1.5f);
        if (ray && hit.distance > 0)
        {
            var targetPos = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            rb.MovePosition(Vector3.Lerp(transform.position, targetPos, deltaTime * 10f));
        }
    }

    /// <summary>
    /// 海に落ちる直前の位置を更新
    /// </summary>
    /// <param name="position">プレイヤーの位置</param>
    public void UpdatePositionBeforeWaterFall(Vector3 position)
    {
        // 地面についていれば、位置を更新
        if (_isGrounded) PositionBeforeWaterFall = position;
    }
}