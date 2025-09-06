using Fusion;
using InGame.Common;
using InGame.Exhibit.InteractEffect;
using InGame.Health;
using InGame.Interact;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ability.Effect
{
    public class AbilityHammerAttack : AbilityBase
    {
        [SerializeField] private AnimationClip _normalAttackAnimationClip;
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private HitChecker _hitChecker;
        private int _attackDamage = int.MaxValue;
        
        protected override void OnStart()
        {
            var ownerAnimator = Parameter.Owner.GetComponent<AnimationClipPlayer>();
            if (ownerAnimator && Parameter.Owner.HasInputAuthority)
            {
                //ownerAnimator.PlayClip(_normalAttackAnimationClip, 1, 0, true);
            }
            
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
            if (_hitChecker.IsActive)
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