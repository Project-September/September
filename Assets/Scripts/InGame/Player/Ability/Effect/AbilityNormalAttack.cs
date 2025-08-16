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
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private int _attackDamage = 10;
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
            Debug.Log($"HitPoints: StartFrame: {start}, EndFrame: {end}, Radius: {radius}");
            _executor = new MeleeHitboxExecutor(points, radius, _hitMask, start ?? 0, end ?? int.MaxValue)
            {
                OnHit = collider =>
                {
                    //自分に当たった場合は無視
                    if (collider.GetComponentInParent<NetworkObject>() == Parameter.Owner) return;
                    var damageable = collider.GetComponentInParent<IDamageable>();
                    if (damageable == null) return;
                    var hitData = new HitData(
                        HitActionType.Damage,
                        _attackDamage,
                        Parameter.Owner.InputAuthority,
                        damageable.OwnerPlayerRef);
                    damageable.TakeHit(ref hitData);
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