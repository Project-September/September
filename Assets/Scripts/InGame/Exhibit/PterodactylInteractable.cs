using System;
using System.Threading;
using CRISound;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using InGame.Interact;
using NaughtyAttributes;
using September.Common;
using September.InGame.Effect;
using UnityEngine;
using UnityEngine.Serialization;

namespace InGame.Exhibit
{
    public class PterodactylInteractable : MountableExhibitBase
    {
        [Header("Movement Settings")] [SerializeField]
        private float _moveSpeed = 5f;

        [SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private float _turnThreshold = 30f;

        [Header("BulletSettings")] [SerializeField]
        private Transform _muzzle;

        [SerializeField] private ParticleSystem _muzzleFlash;
        [SerializeField, Label("Fireの方向")] private float _downwardAngle = 30f;
        [SerializeField] private NetworkObject _fireParticle;
        [SerializeField] private float _fireSpeed = 20f;
        [SerializeField] private float _rayDistance = 100f;

        [FormerlySerializedAs("_fireHitMask")] [SerializeField]
        private LayerMask _playerHitMask;

        [SerializeField] private float _fireCooldown = 2f;
        [SerializeField, Label("爆撃の有効範囲")] private float _radius = 1f;
        [SerializeField] private GameObject _hitEffect;
        [SerializeField] private int _damage;
        [SerializeField] private NetworkObject _aimObject;
        private float _fireCooldownTimerSec;
        private InteractableBase _interactableBase;
        private const float Threshold = 0.02f;
        private const float LerpSpeed = 5f;
        private float _currentTargetValue;
        private NetworkMecanimAnimator _mecanimAnimator;
        private CancellationTokenSource _attackCts;
        private PlayerRef _owner;
        private LayerMask _groundLayer;
        private Vector3 _hitPosition;

        [Networked] private Vector3 AimObjectPosition { get; set; }
        [Networked] private Quaternion AimObjectRotation { get; set; }
        
        [Networked, OnChangedRender(nameof(OnChangeAnimation))]
        private float CurrentBlendValue { get; set; }
        
        [Networked, OnChangedRender(nameof(OnAimObjectActiveChanged))]
        private NetworkBool IsAimObjectActive { get; set; }

        [Networked, OnChangedRender(nameof(OnInteractingChanged))]
        private NetworkBool IsInteracting { get; set; }
        [Networked, OnChangedRender(nameof(OnAttackTriggered))]
        private NetworkBool AttackTrigger { get; set; }

        #region AnimationHash

        private static readonly int FlyStateBlend = Animator.StringToHash("FlyStateBlend");
        private static readonly int Attack = Animator.StringToHash("Attack");

        #endregion

        public override void Spawned()
        {
            base.Spawned();
            _interactableBase = GetComponent<InteractableBase>();
            _mecanimAnimator = GetComponent<NetworkMecanimAnimator>();
            Animator.enabled = IsInteracting;
            IsAimObjectActive = false;
        }

        public override void Render()
        {
            Animator.enabled = IsInteracting;

            float baseMin = IsInteracting ? _currentTargetValue : 0f;
            float clamped = Mathf.Max(CurrentBlendValue, baseMin);
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
            _owner = ownerPlayerRef;
            IsAimObjectActive = true;
        }

        public override void GetOff(PlayerRef ownerPlayerRef)
        {
            base.GetOff(ownerPlayerRef);

            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            IsInteracting = false;
            _currentTargetValue = 0f;
            CurrentBlendValue = _currentTargetValue;
            _interactableBase.ForceSetInteractable = true;
            _owner = PlayerRef.None;
            IsAimObjectActive = false;
        }

        public override void OnInteractFixedUpdate(PlayerInput playerInput, float deltaTime)
        {
            if (!HasStateAuthority)
                return;

            HandleMovement(playerInput);
            _fireCooldownTimerSec = Mathf.Min(_fireCooldown, _fireCooldownTimerSec + deltaTime);
            
            if (_fireCooldownTimerSec >= _fireCooldown && AttackTrigger)
            {
                // クールタイム満了 → トリガーをリセット
                AttackTrigger = false;
            }
            
            var hit = GetAimPosition();
            if (playerInput.Buttons.IsSet(PlayerButtons.Attack) && _fireCooldownTimerSec >= _fireCooldown)
            {
                _attackCts?.Cancel();
                RPC_SendHitPoint(hit.point);
                Fire(_owner, _hitPosition);
            }

            _aimObject.transform.position = AimObjectPosition;
            _aimObject.transform.rotation = AimObjectRotation;
        }

        [Rpc]
        private void RPC_SendHitPoint(Vector3 hitPoint)
        {
            _hitPosition = hitPoint;
        }

        private RaycastHit GetAimPosition()
        {
            RaycastHit[] hits = new RaycastHit[30];

            Vector3 angleForward = Quaternion.AngleAxis(_downwardAngle, _muzzle.right) * _muzzle.forward;

            var count = Physics.RaycastNonAlloc(_muzzle.position, angleForward, hits, _rayDistance);

            for (int i = 0; i < count; i++)
            {
                // ヒットしたオブジェクトのTransformを取得
                Transform hitTransform = hits[i].transform;
                // ヒットしたオブジェクトが自分自身、または自分の子オブジェクトか確認
                if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
                var point = hits[i].point;
                if (!IsAimObjectActive)
                {
                    IsAimObjectActive = true;
                }
                var offset = hits[i].normal * 1f;
                AimObjectPosition = point + offset;
                var rot = Quaternion.FromToRotation(Vector3.up,hits[i].normal);
                AimObjectRotation = rot;
                return hits[i];
            }

            if (IsAimObjectActive)
            {
                IsAimObjectActive = false;
            }

            return new RaycastHit();
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

            float src = CurrentBlendValue;
            float next = Mathf.Lerp(src, target, Runner.DeltaTime * LerpSpeed);

            if (Mathf.Abs(next - src) >= Threshold)
                CurrentBlendValue = next;
        }

        private void Fire(PlayerRef ownerPlayerRef, Vector3 hitPosition)
        {
            _attackCts = new CancellationTokenSource();
            if (_muzzle == null)
            {
                Debug.LogError("Muzzle is null");
                return;
            }

            _fireCooldownTimerSec = 0f;
            // 弾を生成し、ターゲット方向に飛ばす
            Vector3 dir = (hitPosition - _muzzle.position).normalized;
            Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
            Vector3 velocity = dir * _fireSpeed;
            var travelTime = Vector3.Distance(_muzzle.position, hitPosition) / _fireSpeed;
            PlayFireBullet();
            var instance = Runner.Spawn(_fireParticle, _muzzle.position, rotation);
            if (instance.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = velocity;
            }

            AttackTrigger = true;
            AttackAsync(instance, travelTime, hitPosition, ownerPlayerRef).Forget();
        }
        
        private void OnAttackTriggered()
        {
            if (!AttackTrigger) 
                return;
            
            _mecanimAnimator.SetTrigger(Attack);
            PlayMuzzleFlash(_muzzle.position, _muzzle.rotation);
        }

        private async UniTaskVoid AttackAsync(NetworkObject explosion, float time, Vector3 point,
            PlayerRef ownerPlayerRef)
        {
            if (!Runner.IsServer) return;
            await UniTask.Delay(TimeSpan.FromSeconds(time), cancellationToken: _attackCts.Token);
            var effect = Runner.Spawn(_hitEffect, point, Quaternion.identity);
            Destroy(effect.gameObject, 3f);
            Runner.Despawn(explosion);
            // 当たったものがPlayerであればDamageを与える
            var cols = Physics.OverlapSphere(point, _radius, _playerHitMask);
            foreach (var col in cols)
            {
                var damageable = col.GetComponentInParent<IDamageable>();
                var hitData = new HitData(HitActionType.Damage, _damage, ownerPlayerRef, damageable.OwnerPlayerRef);
                damageable.TakeHit(ref hitData);
            }
        }

        private void PlayMuzzleFlash(Vector3 pos, Quaternion rot)
        {
            StaticServiceLocator.Instance.Get<EffectSpawner>()
                .RequestPlayOneShotEffect(EffectType.PtrFireMuzzle, pos, rot, null);

            if (_muzzleFlash != null)
                _muzzleFlash.Play();
        }

        private void PlayFireBullet()
        {
            _mecanimAnimator.SetTrigger(Attack);
            PlayMuzzleFlash(_muzzle.position, _muzzle.rotation);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _attackCts?.Dispose();
        }
        
        private void OnAimObjectActiveChanged()
        {
            // ネットワーク変数の変更をすべてのクライアントに反映
            _aimObject.gameObject.SetActive(IsAimObjectActive);
        }
    }
}