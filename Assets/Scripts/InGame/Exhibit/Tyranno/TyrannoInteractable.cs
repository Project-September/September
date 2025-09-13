using Fusion;
using InGame.Exhibit;
using September.Common;
using UnityEngine;

public class TyrannoInteractable : MountableExhibitBase
{
    [SerializeField] private float _walkSpeed;
    [SerializeField] private float _dashSpeed;
    [SerializeField] private Vector3 _rayDirection;
    [SerializeField] private float _rayDistance;
    [SerializeField] private Vector3 _gravity;
    [SerializeField] private float _maxRotateValue;
    
    private Vector3 _groundNormal;

    private bool _isGround;

    private bool _isAttacking;

    private float _hitDistance;

    private float _movingTime;
    
    [Networked, OnChangedRender(nameof(OnBlendChangedRender))]
    private float MoveValue { get; set; }

    [Networked, OnChangedRender(nameof(SetIsInteracting))]
    private bool IsInteracting { get; set; }

    private NetworkMecanimAnimator _mecanimAnimator;
    
    private static readonly int Attack = Animator.StringToHash("Attack");
    
    public override void Spawned()
    {
        base.Spawned();
        _mecanimAnimator = GetComponent<NetworkMecanimAnimator>();
    }

    public override void GetOn(PlayerRef playerRef)
    {
        base.GetOn(playerRef);
        IsInteracting = true;
        HitAction += OnHit;
    }

    public override void GetOff(PlayerRef playerRef)
    {
        base.GetOff(playerRef);
        IsInteracting = false;
        HitAction -= OnHit;
    }

    public override void OnInteractFixedUpdate(PlayerInput playerInput, float deltaTime)
    {
        CheckIsGround();
        var moveDirection = Move(playerInput);
        moveDirection.y = 0;　
        Rotate(deltaTime, moveDirection);　//回転
        AdsorptionOnGround(deltaTime);　//自身が浮いてしまったときに補正して地面にくっつける
        AttackAnimationTrigger(playerInput);　//攻撃のAnimationを発火
        OnAttackUpdate(deltaTime);　//Attack中にだけ発火するメソッド
    }

    private void OnHit()
    {
        _mecanimAnimator.SetTrigger("Hit");
    }

    private void AttackAnimationTrigger(PlayerInput playerInput)
    {
        if (!playerInput.Buttons.IsSet(PlayerButtons.Attack)) return;
        _isAttacking = true;
        _mecanimAnimator.SetTrigger(Attack);
    }

    private void OnAttackUpdate(float deltaTime)
    {
        if (!_isAttacking) return;
        Executor?.Tick(deltaTime);
        //if (Executor is not { IsFinished: true }) return;
        _isAttacking = false;
        Executor.Init();
    }

    private void CheckIsGround()
    {
        bool ray = Physics.Raycast(transform.position + Vector3.up, _rayDirection, out RaycastHit hit, _rayDistance);
        var normal = hit.normal;
        if (ray && Vector3.Angle(normal, Vector3.up) < 90)
        {
            _isGround = true;
            _groundNormal = normal;
            return;
        }

        if (!ray || Vector3.Angle(normal, Vector3.up) >= 90)
        {
            _isGround = false;
            Rigidbody.AddForce(_gravity, ForceMode.Acceleration);
        }
    }

    /// <summary> カメラ視点の移動入力を取得 </summary>
    Vector2 GetMoveDirection(Vector2 moveInput, float cameraYaw)
    {
        var radYaw = -cameraYaw * Mathf.Deg2Rad;
        return new Vector2(
            moveInput.x * Mathf.Cos(radYaw) - moveInput.y * Mathf.Sin(radYaw),
            moveInput.x * Mathf.Sin(radYaw) + moveInput.y * Mathf.Cos(radYaw)
        );
    }

    private Vector3 Move(PlayerInput playerInput)
    {
        var inputMoveDirection = playerInput.MoveDirection;
        if (playerInput.MoveDirection == Vector2.zero)
        {
            MoveValue = 0;
            return Vector3.zero;
        }
        var moveVector2 = GetMoveDirection(inputMoveDirection, playerInput.CameraYaw);
        var moveDirection = new Vector3(moveVector2.x, 0, moveVector2.y);
        var moveVelocity = Vector3.ProjectOnPlane(moveDirection, _groundNormal).normalized;　//坂に沿った動きに
        if (playerInput.Buttons.IsSet(PlayerButtons.Dash))
        {
            MoveValue = 1;
            Rigidbody.linearVelocity = moveVelocity * _dashSpeed;
        }
        else
        {
            MoveValue = 0.5f;
            Rigidbody.linearVelocity = moveVelocity * _walkSpeed;
        }
        return moveDirection;
    }
    private void Rotate(float deltaTime, Vector3 moveDirection)
    {
        if (moveDirection == Vector3.zero) return;
        var rot = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moveDirection),
            _maxRotateValue * deltaTime);
        transform.rotation = rot;
    }

    private void AdsorptionOnGround(float deltaTime)
    {
        if (_isGround) return;
        var ray = Physics.Raycast(transform.position + Vector3.up, _rayDirection, out RaycastHit hit,
            1.5f);
        if (ray && hit.distance > 0)
        {
            var targetPos = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            Rigidbody.MovePosition(Vector3.Lerp(transform.position, targetPos, deltaTime * 10f));
        }
    }

    public void OnBlendChangedRender()
    {
        Animator.SetFloat("Blend", MoveValue);
    }

    public void SetIsInteracting()
    {
        Animator.SetBool("IsInteracting", IsInteracting);
    }
}