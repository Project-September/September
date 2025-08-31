using CRISound;
using Fusion;
using InGame.Interact;
using NaughtyAttributes;
using September.Common;
using September.InGame.Effect;
using UnityEngine;

namespace InGame.Exhibit
{
    public class PterodactylInteractable : MountableExhibitBase
    {
        [Header("Sound Settings")] 
        [SerializeField] private string _crySe = "Pteranodon_cry";

        [Header("Movement Settings")] 
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private float _turnThreshold = 30f;

        [Header("BulletSettings")]
        [SerializeField] private Transform _muzzle;
        [SerializeField] private ParticleSystem _muzzleFlash;
        [SerializeField, Label("Fireの方向")] private float _downwardAngle = 30f;
        [SerializeField] private GameObject _fireParticle;
        [SerializeField] private float _fireSpeed = 20f;
        [SerializeField] private float _rayDistance = 100f;
        [SerializeField] private LayerMask _fireHitMask;
        [SerializeField] private float _fireCooldown = 2f;
        
        private float _fireCooldownTimerSec;
        private InteractableBase _interactableBase;
        private const float Threshold = 0.02f;
        private const float LerpSpeed = 5f;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private float _currentTargetValue;
        private NetworkMecanimAnimator _mecanimAnimator;

        [Networked, OnChangedRender(nameof(OnChangeAnimation))]
        private float CurrentBlendValue { get; set; }
        [Networked,OnChangedRender(nameof(OnInteractingChanged))]
        private NetworkBool IsInteracting { get; set; }

        #region AnimationHash

        private static readonly int FlyStateBlend = Animator.StringToHash("FlyStateBlend");
        private static readonly int Attack = Animator.StringToHash("Attack");

        #endregion

        public override void Spawned()
        {
            base.Spawned();
            
            _interactableBase = GetComponent<InteractableBase>();
            _initialPosition  = transform.position;
            _initialRotation  = transform.rotation;
            
            if (HasStateAuthority)
                Rigidbody.isKinematic = false;
            
            _mecanimAnimator =  GetComponent<NetworkMecanimAnimator>();
            Animator.enabled = IsInteracting;
        }

        public override void Render()
        {
            Animator.enabled = IsInteracting;

            float baseMin = IsInteracting ? _currentTargetValue : 0f;
            float clamped  = Mathf.Max(CurrentBlendValue, baseMin);
            Animator.SetFloat(FlyStateBlend, clamped);
        }

        public override void GetOn(PlayerRef ownerPlayerRef)
        {
            if (!Runner.IsServer || OwnerPlayerRef != PlayerRef.None)
                return;

            base.GetOn(ownerPlayerRef);
            IsInteracting = true; 
            
            _currentTargetValue = 0.01f;
            CurrentBlendValue = _currentTargetValue;
            _interactableBase.ForceSetInteractable = false;
            OnPlaySE(_crySe);
        }

        public override void GetOff(PlayerRef ownerPlayerRef)
        {
            base.GetOff(ownerPlayerRef);

            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(_initialPosition,_initialRotation);
            
            IsInteracting = false; 
            _currentTargetValue = 0f;
            CurrentBlendValue = _currentTargetValue;
            _interactableBase.ForceSetInteractable = true;
        }

        public override void OnInteractFixedUpdate(PlayerInput playerInput, float deltaTime)
        {
            if (!HasStateAuthority) 
                return;
            
            HandleMovement(playerInput);
            _fireCooldownTimerSec = Mathf.Min(_fireCooldown, _fireCooldownTimerSec + deltaTime);
                
            if (playerInput.Buttons.IsSet(PlayerButtons.Attack) && _fireCooldownTimerSec >= _fireCooldown)
            {
                Fire();
            }
        }

        // Animationの更新処理
        private void OnChangeAnimation()
        {
            float clampedBlend = Mathf.Clamp(CurrentBlendValue, _currentTargetValue, 1f);
            Animator.SetFloat(FlyStateBlend, clampedBlend);
        }

        private void OnInteractingChanged()
        {
            Animator.enabled = IsInteracting;
        }

        private void HandleMovement(PlayerInput input)
        {
            Vector2 moveInput = input.MoveDirection;

            if (moveInput == Vector2.zero)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                
                float idleTarget = IsInteracting ? _currentTargetValue : 0;
                SetBlendValue(idleTarget);
                return;
            }

            Vector3 moveDir = CalculateMoveDirection(input);
            // キャラの回転方向の決定
            float angleToMoveDir = Vector3.SignedAngle(transform.forward, moveDir, Vector3.up);
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Runner.DeltaTime * _rotationSpeed);

            // 後ろ方向のときは無理な移動をしないように制御
            if (moveInput.y < 0 && Mathf.Abs(angleToMoveDir) > _turnThreshold)
                Rigidbody.linearVelocity = Vector3.zero;
            else
                Rigidbody.linearVelocity = moveDir * _moveSpeed;

            SetBlendValue(moveInput.magnitude);
        }

        // 向きたい方向を取得
        private Vector3 CalculateMoveDirection(PlayerInput input)
        {
            Vector3 lookDir = input.DesiredLookDirection.normalized;
            Vector3 cameraRight = Vector3.Cross(Vector3.up, lookDir);
            return (lookDir * input.MoveDirection.y + cameraRight * input.MoveDirection.x).normalized;
        }

        private void SetBlendValue(float target)
        {
            if (IsInteracting) 
                target = Mathf.Max(target, _currentTargetValue);

            float src  = CurrentBlendValue;
            float next = Mathf.Lerp(src, target, Runner.DeltaTime * LerpSpeed);

            if (Mathf.Abs(next - src) >= Threshold)
                CurrentBlendValue = next;
        }

        private void Fire()
        {
            if (_muzzle == null)
            {
                Debug.LogError("Muzzle is null");
                return;
            }
            
            Vector3 angleForward = Quaternion.AngleAxis(_downwardAngle, _muzzle.right) * _muzzle.forward;
            
            if (Physics.Raycast(_muzzle.position, angleForward, out RaycastHit hit, _rayDistance,
                    _fireHitMask))
            {
                // 弾を生成し、ターゲット方向に飛ばす
                Vector3 dir = (hit.point - _muzzle.position).normalized;
                Quaternion rotation = Quaternion.LookRotation(dir,Vector3.up);
                Vector3 velocity = dir * _fireSpeed;
                
                Rpc_PlayFireBullet(_muzzle.position, rotation, velocity);
            }
            else
                Debug.LogError("No hit found in angled ray");

            _fireCooldownTimerSec = 0f;
        }
        
        private void PlayMuzzleFlash(Vector3 pos, Quaternion rot)
        {
            StaticServiceLocator.Instance.Get<EffectSpawner>()
                .RequestPlayOneShotEffect(EffectType.PtrFireMuzzle, pos, rot, null);

            if (_muzzleFlash != null)
                _muzzleFlash.Play();
        }

        [Rpc]
        private void Rpc_PlayFireBullet(Vector3 spawnPos, Quaternion rotation, Vector3 initialVelocity)
        {
            if(!_fireParticle)
                return;
            
            _mecanimAnimator.SetTrigger(Attack);
            PlayMuzzleFlash(_muzzle.position, _muzzle.rotation);
            
            GameObject instance = Instantiate(_fireParticle,spawnPos, rotation);

            if (instance.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = initialVelocity;
            }
        }

        // サウンド設定
        private void OnPlaySE(string cueName)
        {
            RPC_PlaySE(transform.position, cueName);
        }

        [Rpc]
        private void RPC_PlaySE(Vector3 position, string cueName)
        {
            CRIAudio.PlaySE(position, "Exhibit", cueName);
        }
    }
}