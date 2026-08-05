using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
using September.InGame.Kraken.Attack;
using UnityEngine;

namespace September.InGame.Kraken.Animations
{
    /// <summary>
    /// 触手攻撃を管理するクラス
    /// </summary>
    public class KrakenAttackHandler : NetworkBehaviour
    {
        [Header("触手設定")]
        [SerializeField] private KrakenTentacleSettings _tentacle;

        [Header("攻撃設定")]
        [SerializeField] private float _hitRayCastHeight;
        [SerializeField] private float _areaHitDuration;
        [SerializeField] private float _areaHitRadius;
        [SerializeField] private int _damage;
        [SerializeField] private LayerMask _hitGroundLayer;
        [SerializeField] private LayerMask _attackTargetLayer;
        [SerializeField] private float _armStartTime;
        [SerializeField] private float _armEndTime;

        public bool IsReady => _tentacle.TryGetUnusedArm(out _);

        private void Start()
        {
            foreach (ArmSettings arm in _tentacle.Arms)
            {
                arm.ArmHitChecker.OnHit += col => OnHitAction(arm.AlreadyHits, col);
            }
        }

        public bool TryGetArm(out ArmSettings arm)
        {
            return _tentacle.TryGetUnusedArm(out arm);
        }

        public async UniTask Attack(ArmSettings arm, Vector3 target)
        {
            Debug.Log("Start Attack");
            _tentacle.StartUseArm(arm);
            _tentacle.LookAt(arm, target);

            await UniTask.WhenAll(
                _tentacle.PlayAnimation(arm, destroyCancellationToken),
                HitCheck(arm.ArmHitChecker, _armStartTime, _armEndTime, destroyCancellationToken));

            _tentacle.ReleaseUsingArm(arm);
            Debug.Log("End Attack");
        }

        public void StartAreaAttack(ArmSettings arm, PredictionParticle particle)
        {
            arm.AreaHitCapsule =
                HitCapsule.Create(
                    Runner,
                    new CapsuleShape(particle.ForwardEndPos, particle.BackEndPos, _areaHitRadius),
                    _areaHitDuration,
                    _attackTargetLayer,
                    col => OnHitAction(arm.AlreadyHits, col)
                );
        }

        public void TickAreaAttack()
        {
            foreach (ArmSettings arm in _tentacle.Arms)
            {
                if (!arm.AreaHitCapsule.ExpiredOrNotRunning(Runner))
                {
                    arm.AreaHitCapsule.Cast();
                }
            }
        }

        private static async UniTask HitCheck(HitChecker hitChecker, float startTime, float endTime, CancellationToken token = default)
        {
            await UniTask.WaitForSeconds(startTime, cancellationToken: token);
            hitChecker.StartHitCheck();
            await UniTask.WaitForSeconds(endTime - startTime, cancellationToken: token);
            hitChecker.EndHitCheck();
        }

        private void OnHitAction(HashSet<Collider> alreadyHits, Collider hitCollider)
        {
            if (alreadyHits.Contains(hitCollider)) return;

            float rayWorldHeight = _hitRayCastHeight + transform.position.y;
            Vector3 hitPos = hitCollider.ClosestPoint(transform.position);
            Vector3 rayOrigin = new(hitPos.x, rayWorldHeight, hitPos.z);
            float distance = rayWorldHeight - hitPos.y;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, distance, _hitGroundLayer))
            {
                Debug.DrawRay(rayOrigin, Vector3.down * distance, Color.red, 100f);
                return;
            }

            Debug.DrawRay(rayOrigin, Vector3.down * distance, Color.green, 100f);

            alreadyHits.Add(hitCollider);

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                var hitData = new HitData { HitActionType = HitActionType.Damage, Amount = _damage, ExecutorRef = Object.InputAuthority, TargetRef = damageable.OwnerPlayerRef };

                damageable.TakeHit(ref hitData);
            }
        }

        /// <summary>
        /// 触手リポジトリ兼アニメーター
        /// </summary>
        [Serializable]
        private class KrakenTentacleSettings
        {
            [SerializeField] private ArmSettings[] _arms;
            [SerializeField] private string _animationName;
            [SerializeField] private string _endStateName;

            private HashSet<ArmSettings> _usingArms = new();

            public IReadOnlyList<ArmSettings> Arms => _arms;

            public bool TryGetUnusedArm(out ArmSettings result)
            {
                foreach (ArmSettings arm in _arms)
                {
                    if (_usingArms.Contains(arm)) continue;
                    result = arm;
                    return true;
                }

                result = null;
                return false;
            }

            public void StartUseArm(ArmSettings arm)
            {
                _usingArms.Add(arm);
            }

            public void ReleaseUsingArm(ArmSettings arm)
            {
                arm.AlreadyHits.Clear();
                _usingArms.Remove(arm);
            }

            public void LookAt(ArmSettings arm, Vector3 target)
            {
                var root = arm.ArmRoot;

                arm.StartRotation = root.rotation;

                var forward = -root.transform.right;
                forward.y = 0;
                var dir = Vector3.ProjectOnPlane(target - root.position, Vector3.up).normalized;
                Debug.DrawRay(root.position, dir * 100f, Color.red, 3f);

                var rot = Quaternion.FromToRotation(forward, dir);
                Debug.Log($"{target} {forward} {dir} {rot.eulerAngles}", arm.ArmRoot);

                root.rotation *= rot;
            }

            public async UniTask PlayAnimation(ArmSettings arm, CancellationToken token = default)
            {
                await arm.Animator.PlayAsync(_animationName, 0, 0f, cancellationToken: token);
                await arm.Animator.WaitState(_endStateName, cancellationToken: token);
                arm.Animator.transform.rotation = arm.StartRotation;
                Debug.Log($"{_animationName} {arm.StartRotation.eulerAngles}", arm.ArmRoot);
            }
        }
    }
}
