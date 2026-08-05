using System;
using System.Collections.Generic;
using Fusion;
using InGame.Health;
using September.InGame.Kraken.Attack;
using UnityEngine;

namespace September.InGame.Kraken.Animations
{
    public class TentacleController
    {
        private readonly ArmSettings _armSettings;
        private readonly KrakenSettings _krakenSettings;

        public TentacleController(ArmSettings armSettings, KrakenSettings krakenSettings)
        {
            _armSettings = armSettings;
            _krakenSettings = krakenSettings;

            armSettings.ArmHitChecker.OnHit += col => OnHitAction(armSettings.AlreadyHits, col);
        }

        public void StartAttack(NetworkRunner runner)
        {
            _armSettings.IsAttacking = true;
            _armSettings.StartAttackTick = runner.Tick;
        }

        public void StartAreaAttack(NetworkRunner runner, PredictionParticle particle)
        {
            _armSettings.AreaHitCapsule =
                HitCapsule.Create(
                    runner,
                    new CapsuleShape(particle.ForwardEndPos, particle.BackEndPos, _krakenSettings.AreaHitRadius),
                    _krakenSettings.AreaHitDuration,
                    _krakenSettings.AttackTargetLayer,
                    col => OnHitAction(_armSettings.AlreadyHits, col)
                );
        }

        public void Tick(NetworkRunner runner)
        {
            UpdatePhysicsState(runner);
            UpdateArmAttackState(runner);
            UpdateAreaAttackState(runner);
        }

        private void UpdatePhysicsState(NetworkRunner runner)
        {
            int targetTick = (int)(_krakenSettings.TentacleEnablePhysicsTime * runner.Tick);
            int elapsedTick = runner.Tick - _armSettings.StartAttackTick;
            if (elapsedTick > targetTick)
            {
                _armSettings.EnablePhysics = true;
            }
        }

        private void UpdateArmAttackState(NetworkRunner runner)
        {
            int armStartTick = (int)(_krakenSettings.ArmStartTime * runner.TickRate);
            int armEndTick = (int)(_krakenSettings.ArmEndTime * runner.TickRate);

            if (armStartTick < runner.Tick && runner.Tick < armEndTick)
            {
                if (!_armSettings.ArmHitChecker.IsActive) _armSettings.ArmHitChecker.StartHitCheck();
            }
            else
            {
                if (_armSettings.ArmHitChecker.IsActive) _armSettings.ArmHitChecker.EndHitCheck();
            }
        }

        private void UpdateAreaAttackState(NetworkRunner runner)
        {
            if (!_armSettings.AreaHitCapsule.ExpiredOrNotRunning(runner))
            {
                _armSettings.AreaHitCapsule.Cast();
            }
        }

        private void OnHitAction(HashSet<Collider> alreadyHits, Collider hitCollider)
        {
            if (alreadyHits.Contains(hitCollider)) return;

            float rayWorldHeight = _krakenSettings.HitRayCastHeight + _krakenSettings.Root.position.y;
            Vector3 hitPos = hitCollider.ClosestPoint(_krakenSettings.Root.position);
            Vector3 rayOrigin = new(hitPos.x, rayWorldHeight, hitPos.z);
            float distance = rayWorldHeight - hitPos.y;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, distance, _krakenSettings.HitGroundLayer))
            {
                Debug.DrawRay(rayOrigin, Vector3.down * distance, Color.red, 100f);
                return;
            }

            Debug.DrawRay(rayOrigin, Vector3.down * distance, Color.green, 100f);

            alreadyHits.Add(hitCollider);

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                HitData hitData = new()
                {
                    HitActionType = HitActionType.Damage, Amount = _krakenSettings.Damage,
                    ExecutorRef = _krakenSettings.OwnerPlayerRef, TargetRef = damageable.OwnerPlayerRef
                };

                damageable.TakeHit(ref hitData);
            }
        }
    }

    /// <summary>
    /// 触手ごとに持たせるデータ
    /// </summary>
    [Serializable]
    public class ArmSettings
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _armRoot;
        [SerializeField] private HitChecker _armHitChecker;
        [SerializeField] private TentacleConstraintSolver _tentacleConstraintSolver;

        public Animator Animator => _animator;
        public Transform ArmRoot => _armRoot;
        public HitChecker ArmHitChecker => _armHitChecker;
        public HashSet<Collider> AlreadyHits { get; } = new();

        [NonSerialized] public Quaternion StartRotation;
        [NonSerialized] public HitCapsule AreaHitCapsule;
        [NonSerialized] public int StartAttackTick; // Todo: 攻撃開始から数秒間は物理衝突を判定しないようにする
        [NonSerialized] public bool IsAttacking;

        public bool EnablePhysics
        {
            get => _tentacleConstraintSolver.EnablePhysicsConstraint;
            set => _tentacleConstraintSolver.EnablePhysicsConstraint = value;
        }
    }

    [Serializable]
    public class KrakenSettings
    {
        public Transform Root;
        public float TentacleEnablePhysicsTime;

        [Header("攻撃設定")]
        public float HitRayCastHeight;
        public float AreaHitDuration;
        public float AreaHitRadius;
        public int Damage;
        public LayerMask HitGroundLayer;
        public LayerMask AttackTargetLayer;
        public float ArmStartTime;
        public float ArmEndTime;

        [NonSerialized] public PlayerRef OwnerPlayerRef;
    }
}
