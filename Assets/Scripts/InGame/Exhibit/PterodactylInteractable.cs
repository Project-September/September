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
        [SerializeField] private string _crySE = "Pteranodon_cry"; 
        [SerializeField] private string _flapSE = "Pteranodon_Flapping_1";
        
        [Header("Movement Settings")] 
        [SerializeField] private float _moveSpeed; 
        private Rigidbody _rigidbody;
        
        private CameraController _cameraController;
        private float _currentTargetValue;
        private Animator _animator;
        private float _currentSpeed;
        private InteractableBase  _interactableBase;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
          
        [Networked] private float _currentBlendValue { get; set; }
        
        #region AnimationHash
        
        private static readonly int _flyStateBlend = Animator.StringToHash("FlyStateBlend");
        
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
          
        private void LateUpdate()
        {
            if (!HasInputAuthority)
                 return;

            if (GameInput.I.Player.Aim.triggered)
                    _cameraController.CameraReset();
                  
            _cameraController.RotateCamera(GameInput.I.Player.Look.ReadValue<Vector2>(), Time.deltaTime);
        }
          
        public override void GetOn(PlayerRef ownerPlayerRef)
        {
            if(!Runner.IsServer || OwnerPlayerRef != PlayerRef.None)
                return;
              
            base.GetOn(ownerPlayerRef);
          
            RPC_SetAnimatorEnabled(true);
            _currentTargetValue = 0.01f;
            _interactableBase.ForceSetInteractable = false;
            OnPlaySE(_crySE);
        }

        public override void GetOff(PlayerRef ownerPlayerRef)
        {
            base.GetOff(ownerPlayerRef);
            
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            
            // アニメーションリセット
            RPC_SetAnimatorEnabled(false);
            _interactableBase.ForceSetInteractable = true;　
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetAnimatorEnabled(bool isEnabled)
        {
            _animator.enabled = isEnabled;
        }

        public override void OnInteractFixedUpdate(PlayerInput playerInput,float deltaTime)
        {
            if (!HasStateAuthority) 
                return;
            
            Move(playerInput);
            float clampedBlend = Mathf.Clamp(_currentBlendValue, _currentTargetValue, 1f);
            _animator.SetFloat(_flyStateBlend, clampedBlend);
        }

        private Vector3 Move(PlayerInput playerInput)
        {
            Vector2 moveDirection = playerInput.MoveDirection;
            if (playerInput.MoveDirection == Vector2.zero)
                return Vector3.zero;
            
            // カメラ方向
            Vector3 lookDir = playerInput.DesiredLookDirection;
            Vector3 cameraForward = lookDir.normalized;
            
            // カメラの右方向
            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward);
          
            // 入力に応じた移動ベクトル
            Vector3 moveDir = (cameraForward * moveDirection.y + cameraRight * moveDirection.x).normalized;
            
            // ターゲット速度を計算
            Vector3 velocityTarget = moveDir * _moveSpeed;
            Vector3 velocityDelta = velocityTarget - _rigidbody.linearVelocity;
            
            // ToDo : ここAddForceだと動き続けてしまう
            _rigidbody.AddForce(velocityDelta, ForceMode.VelocityChange);
            
            _currentBlendValue = Mathf.Lerp(_currentBlendValue, moveDirection.magnitude, Runner.DeltaTime * 5f);
                  
            if (playerInput.DesiredLookDirection.sqrMagnitude > 0.001f)
            {
                Vector3 flatDir = new(playerInput.DesiredLookDirection.x, 0f, playerInput.DesiredLookDirection.z);
                Quaternion targetRotation = Quaternion.LookRotation(flatDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * 5f);
            }

            return moveDir;
        }
          
        [Rpc(RpcSources.All,RpcTargets.All)]
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
       // private void Awake()
       // {
       //      _cameraController = GetComponent<CameraController>();
       //      _animator = GetComponent<Animator>();
       //      if(_animator is null)
       //          Debug.LogError("Animator is null");
       //      
       //      _rigidbody = GetComponent<Rigidbody>(); 
       //      _cameraController.Init(true);
       //      _currentTargetValue = 0;
       // }
       //
       
       //
       // public override void FixedUpdateNetwork()
       // {
       //     if (!HasStateAuthority) 
       //         return;
       //     
       //     if (GetInput<PlayerInput>(out var input))
       //     {
       //         Vector2 moveDirection = input.MoveDirection;
       //         Move(moveDirection);
       //         
       //         _currentBlendValue = Mathf.Lerp(_currentBlendValue, moveDirection.magnitude, Runner.DeltaTime * 5f);
       //         
       //         if (input.DesiredLookDirection.sqrMagnitude > 0.001f)
       //         {
       //             Vector3 flatDir = new(input.DesiredLookDirection.x, 0f, input.DesiredLookDirection.z);
       //             Quaternion targetRotation = Quaternion.LookRotation(flatDir, Vector3.up);
       //             transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * 5f);
       //         }
       //     }
       // }
       //
       //  private void LateUpdate()
       //  {
       //      if (HasInputAuthority)
       //      {
       //          if (GameInput.I.Player.Aim.triggered)
       //              _cameraController.CameraReset();
       //          
       //          _cameraController.RotateCamera(GameInput.I.Player.Look.ReadValue<Vector2>(), Time.deltaTime);
       //      }
       //
       //      float clampedBlend = Mathf.Clamp(_currentBlendValue, _currentTargetValue, 1f);
       //      _animator.SetFloat(_flyStateBlend, clampedBlend);
       //  }
       //
       //  // キャラクターごとにスキルを変更する
       //  protected override void OnInteract(IInteractableContext context)
       //  {
       //      PlayerRef requester = PlayerRef.FromEncoded(context.Interactor);
       //
       //      if (OwnerPlayerRef == PlayerRef.None)
       //          RPC_RequestGetOn(requester);
       //      else if(OwnerPlayerRef == requester)
       //          GetOff();
       //  }
       //  
       //  [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
       //  private void RPC_RequestGetOn(PlayerRef requester)
       //  {
       //      GetOn(requester);
       //  }
       //  
       //  // 動き周り
       //  private void Move(Vector2 moveDirection)
       //  {
       //      if(_rigidbody.isKinematic)
       //          return;
       //      
       //      if (!GetInput<PlayerInput>(out var input)) 
       //          return;
       //      
       //      Vector3 lookDir = input.DesiredLookDirection;
       //      Vector3 cameraForward = lookDir.normalized;
       //      Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward);
       //
       //      Vector3 moveDir = cameraForward * moveDirection.y + cameraRight * moveDirection.x;
       //
       //      // 飛行挙動
       //      Vector3 velocityTarget = moveDir.normalized * _moveSpeed;
       //      Vector3 velocityDelta = velocityTarget - _rigidbody.linearVelocity;
       //      _rigidbody.AddForce(velocityDelta, ForceMode.VelocityChange);
       //  }
       //  
       //  private void GetOn(PlayerRef ownerPlayerRef)
       //  {
       //      if(!Runner.IsServer || OwnerPlayerRef != PlayerRef.None)
       //          return;
       //
       //      _currentTargetValue = 0.01f;
       //      OwnerPlayerRef = ownerPlayerRef;
       //      Object.AssignInputAuthority(OwnerPlayerRef);
       //      OnPlaySE(_crySE);
       //
       //      _ownerPlayerManager = StaticServiceLocator.Instance.Get<InGameManager>()
       //          .PlayerDataDic[OwnerPlayerRef].GetComponent<PlayerManager>();
       //
       //      _ownerPlayerManager.SetControlState(PlayerManager.PlayerControlState.ForcedControl);
       //      _ownerPlayerManager.RPC_SetColliderActive(false);
       //      _ownerPlayerManager.RPC_SetMeshActive(false);
       //      
       //      _rigidbody.isKinematic = false;
       //  }
       //  
       //  private void GetOff()
       //  {
       //      if (!Runner.IsServer || OwnerPlayerRef == PlayerRef.None) 
       //          return;
       //
       //      _currentTargetValue = 0f;
       //      OwnerPlayerRef = PlayerRef.None;
       //      Object.RemoveInputAuthority();
       //      
       //      _ownerPlayerManager.SetControlState(PlayerManager.PlayerControlState.Normal);
       //      _ownerPlayerManager.RPC_SetColliderActive(true);
       //      _ownerPlayerManager.RPC_SetMeshActive(true);
       //      _ownerPlayerManager.transform.position = _getOffPoint.position;
       //      _rigidbody.isKinematic = true;
       //  }