using System;
using Fusion;
using InGame.Common;
using InGame.Health;
using September.Common;
using September.InGame.Common;
using September.InGame.Effect;
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
        [SerializeField] private bool _isSubscribe = false;

        protected override void OnStart()
        {
            var ownerAnimator = Parameter.Owner.GetComponent<AnimationClipPlayer>();
            if (ownerAnimator && Parameter.Owner.HasInputAuthority && _normalAttackAnimationClip)
            {
                ownerAnimator.PlayClip(_normalAttackAnimationClip, 1, 0, true);
            }
            
        
            if (!_isSubscribe)
            {
                _isSubscribe = true;
                _hitChecker.OnHit += OnHitEnemy;
            }
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
            if (_hitChecker.IsFinished)
            {
                _phase = AbilityPhase.Ending;
            }
        }
    }
}