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
    [SerializeField] AnimationCurve _speedCurve;
    [SerializeField] private float _rayDistance;
    [SerializeField] private float _groundMaximumAngle;
    [SerializeField] private Vector3 _fallGravity;
    [SerializeField] private float _maxRotateValue;
    [SerializeField] private float _groundAdsorptionSpeed;

    [Header("正面衝突判定")]
    [SerializeField] float _forwardRayDistance = 1;
    [SerializeField, Range(0, 90)] float _wallAngle = 90;

    /// <summary>
    /// 海に落ちる直前の位置
    /// </summary>
    public Vector3 PositionBeforeWaterFall { get; private set; }

    private Vector3 _currentGroundNormal; // 現在、接触している地面の法線
    private bool _isGrounded; // プレイヤーが地面に接地しているか
    float _keepMovingTime;

    /// <summary>
    /// 移動処理
    /// </summary>
    /// <param name="playerInput">プレイヤーの入力</param>
    /// <param name="deltaTime">フレーム時間</param>
    /// <param name="rb">プレイヤーのRigidbody</param>
    /// <param name="forward">正面方向</param>
    public void UpdateMovement(PlayerInput playerInput, float deltaTime, Rigidbody rb, Vector3 forward)
    {
        CheckGroundManual(rb);
        // 渡されたベクトルをxz平面に射影
        var moveDirection = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        Move(moveDirection, playerInput, rb, deltaTime);
        Rotate(deltaTime, moveDirection);
        AdsorptionOnGround(deltaTime, rb);
        UpdatePositionBeforeWaterFall(transform.position);
    }

    /// <summary>
    /// Raycastで地面の法線を取得する
    /// </summary>
    /// <param name="rb">プレイヤーのRigidbody</param>
    private void CheckGroundManual(Rigidbody rb)
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
    /// <param name="moveDirection">プレイヤーの移動方向</param>
    /// <param name="playerInput">プレイヤーの入力</param>
    /// <param name="rb">プレイヤーのRigidbody</param>
    private void Move(Vector3 moveDirection, PlayerInput playerInput, Rigidbody rb, float deltaTime)
    {
        if (moveDirection == Vector3.zero) return;

        var moveVelocity = Vector3.ProjectOnPlane(moveDirection, _currentGroundNormal).normalized;　//坂に沿った動きに

        // 前方に壁があるか判定
        var ray = new Ray(transform.position, moveDirection);
        // Rayを飛ばす
        if (Physics.Raycast(ray, out var hit, _forwardRayDistance)
            && Vector3.Dot(hit.normal, Vector3.up) <= Mathf.Cos(_wallAngle * Mathf.Deg2Rad))
        {
            // 坂などは判定しないように内積で壁判定
            _keepMovingTime = 0;
        }
        else
        {
            _keepMovingTime += deltaTime;
        }
        // アニメーションカーブで速度取得
        moveVelocity *= _speedCurve.Evaluate(_keepMovingTime);

        if (playerInput.Buttons.IsSet(PlayerButtons.Dash))
        {
            rb.linearVelocity = moveVelocity * _dashSpeed;
        }
        else
        {
            rb.linearVelocity = moveVelocity * _walkSpeed;
        }
    }

    /// <summary>
    /// 移動方向へ滑らかに回転をする
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <param name="moveDirection">プレイヤーの移動方向</param>
    private void Rotate(float deltaTime, Vector3 moveDirection)
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
    private void AdsorptionOnGround(float deltaTime, Rigidbody rb)
    {
        if (_isGrounded) return;
        var ray = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit,
            1.5f);
        if (ray && hit.distance > 0)
        {
            var targetPos = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            rb.MovePosition(Vector3.Lerp(transform.position, targetPos, deltaTime * _groundAdsorptionSpeed));
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