using Fusion;
using InGame.Common;
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
        private MeleeHitboxExecutor _executor;

        protected override void OnStart()
        {
            var ownerAnimator = Parameter.Owner.GetComponent<AnimationClipPlayer>();
            if (ownerAnimator && Parameter.Owner.HasInputAuthority)
            {
                ownerAnimator.PlayClip(_normalAttackAnimationClip, 1, 0, true);
            }
            var resolver = Parameter.Owner.GetComponentInChildren<HitPointResolver>();
            var points = resolver?.GetPoints();
            var start = resolver?.GetStartFrame();
            var end = resolver?.GetEndFrame();
            var radius = resolver?.GetRadius() ?? 0.1f;
            _executor = new MeleeHitboxExecutor(points, radius, _hitMask, start ?? 0, end ?? int.MaxValue)
            {
                OnHit = collider =>
                {
                    //自分に当たった場合は無視
                    if (collider.GetComponentInParent<NetworkObject>() == Parameter.Owner) return;
                    
                    var damageable = collider.GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        var hitData = new HitData(
                            HitActionType.Damage,
                            _attackDamage,
                            Parameter.Owner.InputAuthority,
                            damageable.OwnerPlayerRef);
                        damageable.TakeHit(ref hitData);
                    }
                    
                    var interactable = collider.GetComponentInParent<InteractableBase>();
                    if (interactable != null)
                    {
                        //Haruとしてインタラクトする
                        var context = new InteractableContext()
                        {
                            Interactor = Parameter.Owner.InputAuthority.RawEncoded,
                            CharacterType = CharacterType.HulkTheButcher,
                        };
                        interactable.Interact(context);
                    }
                    
                    // ヒットエフェクトを再生
                }
            };
        }

        protected override void OnUpdate(float deltaTime)
        {
            _executor.Tick(deltaTime);
            if (_executor.IsFinished)
            {
                _phase = AbilityPhase.Ending;
            }
        }
    }
}
