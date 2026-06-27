using UnityEngine;
using Fusion;
using InGame.Exhibit;
using InGame.Interact;
using September.Common;
using September.InGame.Common;

public class SharkInteractable : MountableExhibitBase
{
    [Header("移動設定"), SerializeField] private float _walkSpeed;
    [SerializeField] private float _dashSpeed;
    [SerializeField] private float _rayDistance;
    [SerializeField] private float _groundMaximumAngle;
    [SerializeField] private Vector3 _fallGravity;
    [SerializeField] private float _maxRotateValue;
    [Header("攻撃のクールダウンタイム"), SerializeField]
    private float _cooldownTime;
    
    /// <summary>
    /// インタラクション中か
    /// <para>true：インタラクション中　false：インタラクション中でない</para>
    /// </summary>
    [Networked] public bool IsSharkInteracting { get; private set; }
    [Networked] private bool IsAttacking { get; set; }
    
    private InteractableBase _interactable;
    
    private Vector3 _groundNormal; // 地面の角度
    private Vector3 _justBeforePosition; // 海に落ちる直前の位置を保持
    private bool _isGround; // 地面についているか
    private float _cooldownTimer; // 攻撃のクールダウンタイマー
    private float _counter;
    
    public override void Spawned()
    {
        base.Spawned();
        _interactable = GetComponent<InteractableBase>();
    }

    public override void GetOn(PlayerRef playerRef)
    {
        base.GetOn(playerRef);
        IsSharkInteracting = true;
        _interactable.ForceSetInteractable = false;
        _cooldownTimer = _cooldownTime;
        _counter = 0;
        _justBeforePosition = transform.position;
    }

    public override void GetOff(PlayerRef playerRef)
    {
        base.GetOff(playerRef);
        _interactable.ForceSetInteractable = true;
        // インタラクトしていたプレイヤーを取得し、海に落ちる直前の位置に移動させる
        var obj = StaticServiceLocator.Instance.Get<InGameManager>()
            .PlayerDataDic[playerRef];
        obj.transform.position = _justBeforePosition;
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
        
        _cooldownTimer = Mathf.Min(_cooldownTime, _cooldownTimer + deltaTime);
        AttackAnimationTrigger(playerInput, OwnerPlayerRef);　//攻撃のAnimationを発火
        OnAttackUpdate(deltaTime);　//Attack中にだけ発火するメソッド

        if (_isGround) // 地面にいるときのみ、位置を保存する
        {
            _justBeforePosition = transform.position;
        }
    }
    
    private void AttackAnimationTrigger(PlayerInput playerInput, PlayerRef playerRef)
    {
        if (!playerInput.Buttons.IsSet(PlayerButtons.Attack)) return;
        RPC_RequestAttack();
        CreateHitBox(playerRef);
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestAttack()
    {
        if (_cooldownTimer < _cooldownTime) return;
        IsAttacking = true;
        _cooldownTimer = 0;
    }

    private void OnAttackUpdate(float deltaTime)
    {
        if (!IsAttacking) return;
        _counter++;
        if (_counter >= StartFrame && _counter <= EndFrame)
        {
            Executor?.Tick(deltaTime);
        }
        
        if (!(_counter >= EndFrame)) return;
        Executor?.Init();
        _counter = 0;
        IsAttacking = false;
        Executor = null;
    }
    
    /// <summary>
    /// Raycastで地面の法線を取得する
    /// </summary>
    private void CheckGroundManual()
    {
        bool ray = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, _rayDistance);
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
        var ray = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit,
            1.5f);
        if (ray && hit.distance > 0)
        {
            var targetPos = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            Rigidbody.MovePosition(Vector3.Lerp(transform.position, targetPos, deltaTime * 10f));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Seaは仮の海のタグで付けているので、あとで変更を行う
        if (other.CompareTag("Sea"))
        {
            IsSharkInteracting = false;
        }
    }
}
