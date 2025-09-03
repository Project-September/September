using CRISound;
using Fusion;
using InGame.Health;
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
        [SerializeField] private float _deathFallImpulse = 8f;
        [SerializeField] private bool _resetGravityGetOff;

        [Header("BulletSettings")]
        [SerializeField] private Transform _muzzle;
        [SerializeField] private ParticleSystem _muzzleFlash;
        [SerializeField, Label("Fireの方向")] private float _downwardAngle = 30f;
        [SerializeField] private GameObject _fireParticle;
        [SerializeField] private float _fireSpeed = 20f;
        [SerializeField] private float _rayDistance = 100f;
        [SerializeField] private LayerMask _playerMask;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private float _fireCooldown = 2f;
        [SerializeField] private int _hitDamage = 50;
        
        private float _fireCooldownTimerSec;
        private InteractableBase _interactableBase;
        private const float Threshold = 0.02f;
        private const float LerpSpeed = 5f;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private float _currentTargetValue;
        private NetworkMecanimAnimator _mecanimAnimator;
        private bool _isDefeated;
        private bool _isFalling;

        [Networked, OnChangedRender(nameof(OnChangeAnimation))]
        private float CurrentBlendValue { get; set; }
        [Networked,OnChangedRender(nameof(OnInteractingChanged))]
        private NetworkBool IsInteracting { get; set; }

        public bool IsDefeated => _isDefeated;
        
        #region AnimationHash

        private static readonly int FlyStateBlend = Animator.StringToHash("FlyStateBlend");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int Damage = Animator.StringToHash("Hit");
        private static readonly int Fall = Animator.StringToHash("Fall");

        #endregion

        public override void Spawned()
        {
            base.Spawned();
            
            _interactableBase = GetComponent<InteractableBase>();
            _mecanimAnimator =  GetComponent<NetworkMecanimAnimator>();
            
            _initialPosition  = transform.position;
            _initialRotation  = transform.rotation;
            _isDefeated =  false;
            _isFalling = false;

            if (HasStateAuthority)
            {
                Rigidbody.isKinematic = false;
                Rigidbody.useGravity = false;
            }
            
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
            _isDefeated = false;
            IsInteracting = true; 
            
            // Damage処理の追加
            HitAction += OnHit;
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
            HitAction -= OnHit;
            
            IsInteracting = false; 
            _currentTargetValue = 0f;
            CurrentBlendValue = _currentTargetValue;
            _interactableBase.ForceSetInteractable = true;
        }

        public override void OnInteractFixedUpdate(PlayerInput playerInput, float deltaTime)
        {
            if (!HasStateAuthority) 
                return;
            if (!_isFalling)
            {
                HandleMovement(playerInput);
            }
            
            _fireCooldownTimerSec = Mathf.Min(_fireCooldown, _fireCooldownTimerSec + deltaTime);
                
            if (playerInput.Buttons.IsSet(PlayerButtons.Attack) && _fireCooldownTimerSec >= _fireCooldown)
            {
                Fire();
                // ラグ保障テスト
                //FireLagComp();
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
            
            if (Physics.Raycast(_muzzle.position, angleForward, out RaycastHit playerHit, _rayDistance, _playerMask))
            {
                Vector3 dir = (playerHit.point - _muzzle.position).normalized;
                Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
                Vector3 velocity = dir * _fireSpeed;
                
                Rpc_PlayFireBullet(_muzzle.position, rotation, velocity);
                
                // Damage処理
                IDamageable damageable = playerHit.collider.GetComponentInParent<IDamageable>();
                Debug.Assert(damageable != null);
                
                // 自分はスキップ
                if (damageable.OwnerPlayerRef != OwnerPlayerRef)
                {
                    HitData hitData = new HitData(
                        HitActionType.Damage,
                        _hitDamage,
                        OwnerPlayerRef,
                        damageable.OwnerPlayerRef
                    );
                    damageable.TakeHit(ref hitData);
                }
            }
            else if (Physics.Raycast(_muzzle.position, angleForward, out RaycastHit hit, _rayDistance,
                    _groundLayer))
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

        #region ラグ保障

        private void FireLagComp()
        {
            if (_muzzle == null)
            {
                Debug.LogError("Muzzle is null");
                return;
            }

            // 斜め前方向（既存と同じ）
            Vector3 angleForward = Quaternion.AngleAxis(_downwardAngle, _muzzle.right) * _muzzle.forward;
            
            if (Runner.LagCompensation.Raycast(
                    _muzzle.position,
                    angleForward,
                    _rayDistance,
                    OwnerPlayerRef,
                    out var lagHit,
                    _playerMask))
            {
                ApplyDamageAndPlayVfx(lagHit.Collider, lagHit.Point);
            }
            else if (Physics.Raycast(_muzzle.position, angleForward, out RaycastHit hit, _rayDistance, _groundLayer))
            {
                ApplyDamageAndPlayVfx(hit.collider, hit.point);
            }
            else
            {
                Debug.LogWarning("LagComp & Physics: No hit found.");
            }

            _fireCooldownTimerSec = 0f;
        }
        
        private void ApplyDamageAndPlayVfx(Collider hitCollider, Vector3 hitPoint)
        {
            var damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.OwnerPlayerRef != OwnerPlayerRef)
            {
                var hitData = new HitData(
                    HitActionType.Damage,
                    _hitDamage,
                    OwnerPlayerRef,
                    damageable.OwnerPlayerRef
                );
                damageable.TakeHit(ref hitData);
            }

            // 見た目用の弾（マズル→命中点 方向）
            Vector3 dir = (hitPoint - _muzzle.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            Vector3 vel = dir * _fireSpeed;
            Rpc_PlayFireBullet(_muzzle.position, rot, vel);
        }

        #endregion
        

        // Damage処理
        private void OnHit()
        {
            Debug.Log("Hit");
            if (IsAlive)
            {
                _mecanimAnimator.SetTrigger(Damage);
            }
            else if(!_isFalling)
            {
                Animator.SetBool(Fall,true);
                StartFalling();
            }
        }

        private void StartFalling()
        {
            _isFalling = true;
            
            Rigidbody.isKinematic = false;
            Rigidbody.useGravity = true;
            
            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.AddForce(Vector3.down * _deathFallImpulse, ForceMode.VelocityChange);
        }

        private void TryMarkDefeated(Collider col)
        {
            if (_isDefeated || !_isFalling || !HasStateAuthority)
                return;

            if (col.gameObject.layer == _groundLayer)
            {
                _isDefeated = true;
                _isFalling = false;
                
                if (_interactableBase) 
                    _interactableBase.ForceSetInteractable = false;
            }
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

        private void OnCollisionEnter(Collision other)
        {
            TryMarkDefeated(other.collider);
        }
        private void OnTriggerEnter(Collider other)
        {
            TryMarkDefeated(other.GetComponent<Collider>());
        }
    }
}