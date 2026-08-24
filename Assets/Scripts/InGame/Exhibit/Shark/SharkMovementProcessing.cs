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
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private Vector3 _forwardGroundRayOriginOffset;
    [SerializeField] private Vector3 _backGroundRayOriginOffset;
    [SerializeField, Min(0)] private int _rayDivideCount;

    [Header("正面衝突判定")]
    [SerializeField] float _forwardRayDistance = 1;
    [SerializeField] LayerMask _wallLayerMask;
    [SerializeField, Range(0, 90)] float _wallAngle = 90;

    /// <summary>
    /// 海に落ちる直前の位置
    /// </summary>
    public Vector3 PositionBeforeWaterFall { get; private set; }

    public float CurrentSpeedRatio { get; private set; }

    private Vector3 _currentGroundNormal; // 現在、接触している地面の法線
    private bool _isGrounded; // プレイヤーが地面に接地しているか
    float _keepMovingTime;

    [Networked] private float LastGroundedTime { get; set; }

    private Vector3 FallVelocity => !_isGrounded ? _fallGravity * (Runner.SimulationTime - LastGroundedTime) : Vector3.zero;

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
    }

    /// <summary>
    /// Raycastで地面の法線を取得する
    /// </summary>
    /// <param name="rb">プレイヤーのRigidbody</param>
    private void CheckGroundManual(Rigidbody rb)
    {
        var forwardRayOrigin = transform.TransformPoint(_forwardGroundRayOriginOffset);
        var backRayOrigin = transform.TransformPoint(_backGroundRayOriginOffset);

        // 地面判定
        for (int i = 0; i < _rayDivideCount + 2; ++i)
        {
            var rayOrigin = Vector3.Lerp(forwardRayOrigin, backRayOrigin, i / (_rayDivideCount + 1f));
            bool isHit = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, _rayDistance, _groundLayerMask);
            if (isHit && Vector3.Angle(groundHit.normal, Vector3.up) < _groundMaximumAngle)
            {
                _isGrounded = true;
                _currentGroundNormal = groundHit.normal;　// 最初に見つかった地面の法線を保存
                PositionBeforeWaterFall = groundHit.point; // 最後に接していた地面の位置を保存
                return;
            }
        }

        // 地面から離れた瞬間に、最後の接地時間を保存
        if (_isGrounded)
        {
            LastGroundedTime = Runner.SimulationTime;
        }

        // 地面が見つからなかった
        _isGrounded = false;
        _currentGroundNormal = Vector3.up;
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

        var moveVelocity = Vector3.ProjectOnPlane(moveDirection, _currentGroundNormal);　//坂に沿った動きに
        if (moveVelocity.y < 0) moveVelocity.y = 0; // 下りの場合は無視（頭側で判定しているので上りとして扱う、下りが必要な場合は尾側で判定する）
        moveVelocity = moveVelocity.normalized;

        Debug.DrawRay(transform.position, _currentGroundNormal * 2, Color.yellow);
        Debug.DrawRay(transform.position, moveVelocity * 5f, Color.cyan);

        // 前方に壁があるか判定
        var ray = new Ray(transform.position, moveDirection);
        // Rayを飛ばす
        if (Physics.Raycast(ray, out var hit, _forwardRayDistance, _wallLayerMask)
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
        float t = _speedCurve.Evaluate(_keepMovingTime);
        float baseSpeed = playerInput.Buttons.IsSet(PlayerButtons.Dash) ? _dashSpeed : _walkSpeed;
        float speed = baseSpeed * t;

        rb.linearVelocity = moveVelocity * speed + FallVelocity;

        CurrentSpeedRatio = speed / _dashSpeed;
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
            1.5f, _groundLayerMask);
        if (ray && hit.distance > 0)
        {
            var targetPos = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            rb.MovePosition(Vector3.Lerp(transform.position, targetPos, deltaTime * _groundAdsorptionSpeed));
        }
    }

    public void OnInteractStart()
    {
        LastGroundedTime = Runner.SimulationTime;
        PositionBeforeWaterFall = transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        var forwardRayOrigin = transform.TransformPoint(_forwardGroundRayOriginOffset);
        var backRayOrigin = transform.TransformPoint(_backGroundRayOriginOffset);

        for (int i = 0; i < _rayDivideCount + 2; ++i)
        {
            var rayOrigin = Vector3.Lerp(forwardRayOrigin, backRayOrigin, i / (_rayDivideCount + 1f));
            Gizmos.DrawRay(rayOrigin, Vector3.down * _rayDistance);
        }
    }
}
