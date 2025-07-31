using CRISound;
using Fusion;
using InGame.Interact;
using InGame.Player;
using September.Common;
using UnityEngine;

namespace InGame.Exhibit
{
    public class PterodactylInteractable : MountableExhibitBase
    { 
        [Header("Sound Settings")] 
        [SerializeField] private string _crySe = "Pteranodon_cry"; 
        [SerializeField] private string _flapSe = "Pteranodon_Flapping_1";
        
        [Header("Movement Settings")] 
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private float _turnThreshold = 30f;
        
        private Rigidbody _rigidbody;
        private CameraController _cameraController;
        private InteractableBase  _interactableBase;
        private Animator _animator;
        
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        
        private float _currentTargetValue;
          
        [Networked] public float CurrentBlendValue { get; set; }
        
        #region AnimationHash
        
        private static readonly int FlyStateBlend = Animator.StringToHash("FlyStateBlend");
        
        #endregion
        
        private void Awake() 
        { 
            _rigidbody = GetComponent<Rigidbody>();
            _cameraController = GetComponent<CameraController>(); 
            _interactableBase = GetComponent<InteractableBase>(); 
            _animator = GetComponent<Animator>();
            
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }
          
        // private void LateUpdate()
        // {
        //     if (!HasInputAuthority)
        //          return;
        //
        //     if (GameInput.I.Player.Aim.triggered)
        //             _cameraController.CameraReset();
        //           
        //     _cameraController.RotateCamera(GameInput.I.Player.Look.ReadValue<Vector2>(), Time.deltaTime);
        // }
          
        public override void GetOn(PlayerRef ownerPlayerRef)
        {
            if(!Runner.IsServer || OwnerPlayerRef != PlayerRef.None)
                return;
              
            base.GetOn(ownerPlayerRef);
          
            RPC_SetAnimatorEnabled(true);
            _currentTargetValue = 0.01f;
            _interactableBase.ForceSetInteractable = false;
            OnPlaySE(_crySe);
        }

        public override void GetOff(PlayerRef ownerPlayerRef)
        {
            base.GetOff(ownerPlayerRef);
            
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            
            RPC_SetAnimatorEnabled(false);
            _interactableBase.ForceSetInteractable = true;　
        }
        
        public override void OnInteractFixedUpdate(PlayerInput playerInput,float deltaTime)
        {
            if (!HasStateAuthority) 
                return;
            
            HandleMovement(playerInput);
            float clampedBlend = Mathf.Clamp(CurrentBlendValue, _currentTargetValue, 1f);
            _animator.SetFloat(FlyStateBlend, clampedBlend);
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetAnimatorEnabled(bool isEnabled)
        {
            _animator.enabled = isEnabled;
        }
        
        private void HandleMovement(PlayerInput input)
        {
            Vector2 moveInput = input.MoveDirection;

            if (moveInput == Vector2.zero)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                CurrentBlendValue = Mathf.Lerp(CurrentBlendValue, 0f, Runner.DeltaTime * 5f);
                return;
            }
            
            Vector3 moveDir = CalculateMoveDirection(input);
            float angleToMoveDir = Vector3.SignedAngle(transform.forward, moveDir, Vector3.up);
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Runner.DeltaTime * _rotationSpeed);
            
            if (moveInput.y < 0 && Mathf.Abs(angleToMoveDir) > _turnThreshold)
                _rigidbody.linearVelocity = Vector3.zero;
            else
                _rigidbody.linearVelocity = moveDir * _moveSpeed;

            CurrentBlendValue = Mathf.Lerp(CurrentBlendValue, moveInput.magnitude, Runner.DeltaTime * 5f);
        }

        private Vector3 CalculateMoveDirection(PlayerInput input)
        {
            Vector3 lookDir = input.DesiredLookDirection.normalized;
            Vector3 cameraRight = Vector3.Cross(Vector3.up, lookDir);
            return (lookDir * input.MoveDirection.y + cameraRight * input.MoveDirection.x).normalized;
        }

          
        [Rpc]
        private void RPC_PlaySE(Vector3 position, string cueName)
        {
            CRIAudio.PlaySE(position,"Exhibit", cueName);
        }
          
        private void OnPlaySE(string cueName)
        {
            RPC_PlaySE(transform.position, cueName);
        }
    }
}