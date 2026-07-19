using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Health;
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
        [SerializeField] private float _hitStartTime;
        [SerializeField] private float _hitEndTime;
        [SerializeField] private int _damage;
        [SerializeField] private LayerMask _hitGroundLayer;
        [SerializeField] private float _armStartTime;
        [SerializeField] private float _armEndTime;

        public Vector3 LatestArmRootPosition { get; private set; }

        public bool IsReady => _tentacle.TryGetUnusedArm(out _);

        private void Start()
        {
            foreach (ArmSettings arm in _tentacle.Arms)
            {
                arm.ArmHitChecker.OnHit += col => OnHitAction(arm.AlreadyHits, col);
                arm.AreaHitChecker.OnHit += col => OnHitAction(arm.AlreadyHits, col);
            }
        }

        public async UniTask Attack(Vector3 target)
        {
            Debug.Log("Start Attack");
            if (!_tentacle.TryGetUnusedArm(out ArmSettings arm)) return;
            _tentacle.StartUseArm(arm);
            LatestArmRootPosition = arm.ArmRoot.position;
            _tentacle.LookAt(arm, target);

            await UniTask.WhenAll(
                _tentacle.PlayAnimation(arm),
                HitCheck(arm.ArmHitChecker, _hitStartTime, _hitEndTime),
                HitCheck(arm.AreaHitChecker, _armStartTime, _armEndTime));

            _tentacle.ReleaseUsingArm(arm);
            Debug.Log("End Attack");
        }

        private static async UniTask HitCheck(HitChecker hitChecker, float startTime, float endTime)
        {
            await UniTask.WaitForSeconds(startTime);
            hitChecker.StartHitCheck();
            await UniTask.WaitForSeconds(endTime - startTime);
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
        /// 触手ごとに持たせるデータ
        /// </summary>
        [Serializable]
        public class ArmSettings
        {
            [SerializeField] private Animator _animator;
            [SerializeField] private Transform _armRoot;
            [SerializeField] private HitChecker _areaHitChecker;
            [SerializeField] private HitChecker _armHitChecker;

            public Quaternion StartRotation;

            public Animator Animator => _animator;
            public Transform ArmRoot => _armRoot;
            public HitChecker AreaHitChecker => _areaHitChecker;
            public HitChecker ArmHitChecker => _armHitChecker;
            public HashSet<Collider> AlreadyHits => new();
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

            public async UniTask PlayAnimation(ArmSettings arm)
            {
                await arm.Animator.PlayAsync(_animationName, 0, 0f);
                await arm.Animator.WaitState(_endStateName);
                arm.Animator.transform.rotation = arm.StartRotation;
                Debug.Log($"{_animationName} {arm.StartRotation.eulerAngles}", arm.ArmRoot);
            }
        }
    }
}
