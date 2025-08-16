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
        private int _attackDamage = int.MaxValue;
        private HitChecker _hitChecker;
        
        protected override void OnStart()
        {
            var ownerAnimator = Parameter.Owner.GetComponent<AnimationClipPlayer>();
            if (ownerAnimator && Parameter.Owner.HasInputAuthority)
            {
                ownerAnimator.PlayClip(_normalAttackAnimationClip, 1, 0, true);
            }

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