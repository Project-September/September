using System.Collections.Generic;
using System.Linq;
using InGame.Health;
using UnityEngine;
using Fusion;
using InGame.Common;
using NaughtyAttributes;
using September.Common;
using September.InGame.Common;

namespace InGame.Player.Ability
{
    [System.Serializable]
    public class AbilityNormalAttack : AbilityBase
    {
        [SerializeField, Label("攻撃力")] private int _attackDamage = 10;
        [SerializeField] private float _attackDuration = 1.0f;
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private AnimationClip _attackAnimationClip;

        private static InGameManager _inGameManager;

        private float _remainingTime;
        private MeleeHitboxExecutor _executor;

        public override bool RunLocal => false;
        public override string DisplayName => "通常攻撃";

        public AbilityNormalAttack()
        {
        }

        public AbilityNormalAttack(AbilityNormalAttack original) : base(original)
        {
            _attackDamage = original._attackDamage;
            _attackDuration = original._attackDuration;
            _hitMask = original._hitMask;
            _attackAnimationClip = original._attackAnimationClip;
        }

        public override AbilityBase Clone(AbilityBase abilityReference) => new AbilityNormalAttack(this);

        public override void OnStartNotifyAll(AbilityContext context)
        {
            var players = Object.FindObjectsByType<AnimationClipPlayer>(FindObjectsSortMode.None);
            var ownerAnimator = players.FirstOrDefault(x => x.Object.InputAuthority == PlayerRef.FromEncoded(context.SourcePlayer));
            if (ownerAnimator)
            {
                ownerAnimator.PlayClip(_attackAnimationClip);
            }
            else
            {
                Debug.LogWarning("アニメーションプレイヤーが見つかりません。通常攻撃のアニメーションを再生できません。");
            }
        }

        protected override void OnStart()
        {
            if (!_inGameManager && !StaticServiceLocator.Instance.TryGet(out _inGameManager))
            {
                Debug.LogError("InGameManagerが見つかりません。通常攻撃を実行できません。");
                ForceEnd();
                return;
            }

            if (!_inGameManager.PlayerDataDic.TryGetValue(PlayerRef.FromEncoded(Context.SourcePlayer),
                    out var playerData))
            {
                Debug.LogError("PlayerDataが見つかりません。通常攻撃を実行できません。");
                ForceEnd();
                return;
            }

            var resolver = playerData.GetComponentInChildren<HitPointResolver>();
            var points = resolver?.GetPoints();
            var start = resolver?.GetStartFrame();
            var end = resolver?.GetEndFrame();
            var radius = resolver?.GetRadius() ?? 0.1f;
            _executor = new MeleeHitboxExecutor(points, radius, _hitMask, start ?? 0, end ?? int.MaxValue)
            {
                OnHit = collider =>
                {
                    var damageable = collider.GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        if (damageable.OwnerPlayerRef == PlayerRef.FromEncoded(Context.SourcePlayer))
                        {
                            // 自分自身にはダメージを与えない
                            return;
                        }
                        var hitData = new HitData(
                            HitActionType.Damage,
                            _attackDamage,
                            PlayerRef.FromEncoded(Context.SourcePlayer),
                            damageable.OwnerPlayerRef);
                        damageable.TakeHit(ref hitData);
                        // ヒットエフェクトを再生
                    }
                }
            };

            _remainingTime = _attackDuration;
        }

        protected override void OnUpdate(float deltaTime)
        {
            _remainingTime -= deltaTime;
            if (_remainingTime <= 0f)
            {
                ForceEnd();
                return;
            }

            _executor?.Tick(deltaTime);
        }
    }
}