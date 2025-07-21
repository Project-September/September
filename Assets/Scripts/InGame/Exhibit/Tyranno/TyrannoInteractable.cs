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
    [SerializeField] private float _maxRotateValue;
    
    private bool _isGround;
    private bool _isAttacking;
    
    private float _hitDistance;
    
    public override void GetOn(PlayerRef playerRef)
    {
        base.GetOn(playerRef);
    }

    public override void GetOff(PlayerRef playerRef)
    {
        base.GetOff(playerRef);
    }
    
    public override void OnInteractFixedUpdate(PlayerInput playerInput,float deltaTime)
    {
        CheckIsGround();
        var moveDirection =  Move(playerInput);
        moveDirection.y = 0;
        Rotate(deltaTime,moveDirection);
        AdsorptionOnGround();
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
        if(!ray || Vector3.Angle(normal, Vector3.up) >= 90)
        {
            _isGround = false;
        }
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
    
    private Vector3 Move(PlayerInput playerInput)
    {
        var inputMoveDirection = playerInput.MoveDirection;
        if (playerInput.MoveDirection == Vector2.zero) return Vector3.zero;
        // Vector3 cameraForward = CameraController.GetCameraForward();
        // Vector3 cameraRight = CameraController.GetCameraRight();
        // Vector3 moveDirection = cameraForward * inputMoveDirection.y + cameraRight * inputMoveDirection.x;
        var moveVector2 = GetMoveDirection(inputMoveDirection, playerInput.CameraYaw);
        var moveDirection = new Vector3(moveVector2.x, 0, moveVector2.y);
        var moveVelocity = Vector3.ProjectOnPlane(moveDirection, _groundNormal).normalized;
        Rigidbody.linearVelocity = moveVelocity * _moveSpeed;
        return moveDirection;
    }
    
    private void Rotate(float deltaTime,Vector3 moveDirection)
    {
        if(moveDirection == Vector3.zero) return;
        var rot = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(moveDirection),
            _maxRotateValue * deltaTime);
        transform.rotation = rot;
    }
    
    private void AdsorptionOnGround()
    {
        if (_isGround) return;
        bool ray = Physics.Raycast(transform.position + Vector3.up, _rayDirection, out RaycastHit hit,
            1.5f);
        if (ray && hit.distance > 0)
        {
            transform.position =new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }
    }

    public void IsInteractingAnimationTrigger(bool isInteracting)
    {
        Animator.SetBool("IsInteracting", isInteracting);
    }
}
