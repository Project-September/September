using System;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Common;
using InGame.Health;
using UnityEngine;

namespace InGame.Player.Ability
{
    [Serializable]
    public class AbilityNormalAttack : AbilityBase
    {
        [SerializeField] private AnimationClip _normalAttackAnimationClip;
        [SerializeField] private int _attackDamage = 10;
        [SerializeField] private HitChecker _hitChecker;
        [SerializeField] private bool _isSubscribe = false;
        [SerializeField] private int _startHitCheckFrame = 17;
        [SerializeField] private int _endHitCheckFrame   = 21;
        [SerializeField] private int _endAttackFrame     = 22;
        [SerializeField] private bool _additiveMotion = false;
        [SerializeField] private LayerInfo.Blend _blendIn;
        [SerializeField] private LayerInfo.Blend _blendOut;

        // 変換後のTickオフセット
        int _startHitTick, _endHitTick, _endAttackTick;

        // 攻撃開始Tick
        int _attackStartTick = -1;
        

        protected override void OnStart()
        {
            var ownerAnimator = Parameter.Owner.GetComponent<AnimationClipPlayer>();
            if (ownerAnimator && Parameter.Owner.HasInputAuthority && _normalAttackAnimationClip)
            {
                ownerAnimator.PlayAsync(_normalAttackAnimationClip, LayerInfo.LayerType.FullBody, 1, _additiveMotion,
                    blendIn: _blendIn, outBlend: _blendOut).Forget();
            }
            
            float fps = _normalAttackAnimationClip ? _normalAttackAnimationClip.frameRate : 60f;
            float dt  = Runner != null ? Runner.DeltaTime : Time.fixedDeltaTime;
            int FrameToTick(int f) => Mathf.RoundToInt((f / fps) / dt);

            _startHitTick  = FrameToTick(_startHitCheckFrame);
            _endHitTick    = FrameToTick(_endHitCheckFrame);
            _endAttackTick = FrameToTick(_endAttackFrame);

            _attackStartTick = Runner != null ? Runner.Tick : 0;
            
            if (!_isSubscribe)
            {
                _isSubscribe = true;
                _hitChecker.OnHit += OnHitEnemy;
            }
            _startHitTick  = FrameToTick(_startHitCheckFrame);
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
            int now    = Runner.Tick;
            int elapsed = now - _attackStartTick;

            // ヒット窓
            bool inWindow = elapsed >= _startHitTick && elapsed < _endHitTick;

            if (_hitChecker != null)
            {
                // 必要な時だけ切り替え（連続呼び出しでも軽いが、不要トグルを避ける）
                if (inWindow && !_hitChecker.IsActive) _hitChecker.StartHitCheck();
                if (!inWindow && _hitChecker.IsActive) _hitChecker.EndHitCheck();
            }

            // 攻撃終了
            if (elapsed >= _endAttackTick)
                _phase = AbilityPhase.Ending;
        }
        
        protected override void OnEndAbility()
        {
            if (_isSubscribe && _hitChecker != null)
            {
                _hitChecker.OnHit -= OnHitEnemy;
                _isSubscribe = false;
            }
            base.OnEndAbility();
        }
    }
}