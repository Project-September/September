using System;
using Fusion;
using InGame.Common;
using InGame.Health;
using September.Common;
using September.InGame.Common;
using UnityEngine;

namespace InGame.Player.Ability
{
    [Serializable]
    public class AbilityNormalAttack : AbilityBase
    {
        [SerializeField] private AnimationClip _normalAttackAnimationClip;
        [SerializeField] private int _attackDamage = 10;
        [SerializeField] private HitChecker _hitChecker;
        [SerializeField] private Animator _ownerAnimator;

        protected override void OnStart()
        {
            Debug.Log("AbilityNormalAttack Start");
            var ownerAnimator = Parameter.Owner.GetComponent<AnimationClipPlayer>();
            if (ownerAnimator && Parameter.Owner.HasInputAuthority && _normalAttackAnimationClip)
            {
                ownerAnimator.PlayClip(_normalAttackAnimationClip, 1, 0, true);
            }
            
            _hitChecker.OnHit -= OnHitEnemy;
            _hitChecker.OnHit += OnHitEnemy;
        }

        private void OnHitEnemy(Collider hitInfo)
        {
            if (hitInfo.GetComponentInParent<NetworkObject>() == Parameter.Owner) return;
            var damageable = hitInfo.GetComponentInParent<IDamageable>();
            if (damageable == null) return;
            var hitData = new HitData(
                HitActionType.Damage,
                _attackDamage,
                Parameter.Owner.InputAuthority,
                damageable.OwnerPlayerRef);
            damageable.TakeHit(ref hitData);
        }

        protected override void OnUpdate(float deltaTime)
        {
            var a = _ownerAnimator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"IsName: {a.IsName(_normalAttackAnimationClip.name)}");
            if (_hitChecker.IsFinished || a.IsName(_normalAttackAnimationClip.name))
            {
                _phase = AbilityPhase.Ending;
            }
        }

        protected override void OnEndAbility()
        {
            _hitChecker.OnHit -= OnHitEnemy;
            base.OnEndAbility();
        }
    }
}