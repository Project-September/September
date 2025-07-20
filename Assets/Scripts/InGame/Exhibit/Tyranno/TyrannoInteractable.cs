using Fusion;
using InGame.Exhibit;
using InGame.Interact;
using InGame.Player;
using September.Common;
using UnityEngine;

public class TyrannoInteractable : MountableExhibitBase
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private Vector3 _groundNormal;
    [SerializeField] private Vector3 _rayDirection;
    [SerializeField] private float _rayDistance;
    [SerializeField] private Vector3 _gravity;
    
    private bool _isGround;
    private bool _isAttacking;
    
    public override void GetOn(PlayerRef playerRef, PlayerStatus playerStatus)
    {
        base.GetOn(playerRef, playerStatus);
    }

    public override void GetOff(PlayerRef playerRef)
    {
        base.GetOff(playerRef);
    }

    protected override void OnInteractFixedUpdate(PlayerInput playerInput,float deltaTime)
    {
        CheckIsGround();
        AddGravity(deltaTime);
        Move(playerInput);
        AnimationTrigger(playerInput);
        OnAttackUpdate(deltaTime);
    }

    private void AnimationTrigger(PlayerInput playerInput)
    {
        Animator.SetBool("Run", playerInput.MoveDirection == Vector2.zero ? false : true);
        if (playerInput.Buttons.IsSet(PlayerButtons.Attack))
        {
            _isAttacking = true;
            Animator.SetTrigger("Attack");
        }
    }
    
    private void OnAttackUpdate(float deltaTime)
    {
        if(!_isAttacking) return;
        Executor?.Tick(deltaTime);
        Debug.Log("Attacking");
        if (Executor.IsFinished)
        {
            _isAttacking = false;
        }
    }
    
    private void AddGravity(float deltaTime)
    {
        Rigidbody.AddForce(_gravity * deltaTime, ForceMode.Acceleration);
    }
    
    private void CheckIsGround()
    {
        bool ray = Physics.Raycast(transform.position + Vector3.up, _rayDirection, out RaycastHit hit,
            _rayDistance);
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
            _groundNormal = Vector3.up;
        }
    }

    private void Move(PlayerInput playerInput)
    {
        var inputMoveDirection = playerInput.MoveDirection;
        if (playerInput.MoveDirection == Vector2.zero) return;
        Vector3 cameraForward = CameraController.GetCameraForward();
        Vector3 cameraRight = CameraController.GetCameraRight();
        Vector3 moveDirection = cameraForward * inputMoveDirection.y + cameraRight * inputMoveDirection.x;
        var moveVeloity = Vector3.ProjectOnPlane(moveDirection, _groundNormal).normalized;
        Rigidbody.linearVelocity = moveVeloity * _moveSpeed;
    }
}
