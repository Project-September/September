using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Common;
using InGame.Health;
using InGame.Player;
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
        [SerializeField] private bool _isSubscribe = false;
        [SerializeField] private int _startHitCheckFrame = 17;
        [SerializeField] private int _endHitCheckFrame   = 21;
        [SerializeField] private int _endAttackFrame     = 22;
        [SerializeField] private bool _isStopWhenAttack = true;
        
        [Header("自動エイム設定")]
        [SerializeField] private bool _enableAutoAim = true;

        // 変換後のTickオフセット
        int _startHitTick, _endHitTick, _endAttackTick;

        // 攻撃開始Tick
        int _attackStartTick = -1;
        
        // 最も近い敵のTransform
        private Transform _closestEnemyTransform;
        private PlayerMovement _playerMovement;
        

        protected override void OnStart()
        {
            var ownerAnimator = Parameter.Owner.GetComponent<AnimationClipPlayer>();
            if (ownerAnimator && Parameter.Owner.HasInputAuthority && _normalAttackAnimationClip)
            {
                ownerAnimator.PlayClip(_normalAttackAnimationClip);
            }
            
            float fps = _normalAttackAnimationClip ? _normalAttackAnimationClip.frameRate : 60f;
            float dt  = Runner != null ? Runner.DeltaTime : Time.fixedDeltaTime;
            int FrameToTick(int f) => Mathf.RoundToInt((f / fps) / dt);

            _startHitTick  = FrameToTick(_startHitCheckFrame);
            _endHitTick    = FrameToTick(_endHitCheckFrame);
            _endAttackTick = FrameToTick(_endAttackFrame);

            _attackStartTick = Runner != null ? Runner.Tick : 0;
            
            // PlayerMovementコンポーネントを取得
            _playerMovement = Parameter.Owner.GetComponent<PlayerMovement>();
            
            // 自動エイムが有効な場合のみ最も近い敵を取得
            if (_enableAutoAim)
            {
                _closestEnemyTransform = GetClosestEnemy();
            }
            
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
            if (_isStopWhenAttack) _playerMovement.Stop();
            int now    = Runner.Tick;
            int elapsed = now - _attackStartTick;

            // 最も近い敵の方向を向く
            if (_closestEnemyTransform != null && _playerMovement != null)
            {
                Vector3 directionToEnemy = (_closestEnemyTransform.position - Parameter.Owner.transform.position).normalized;
                directionToEnemy.y = 0; // Y軸は無視して水平方向のみ
                
                if (directionToEnemy.magnitude > 0.1f)
                {
                    _playerMovement.SetRotationDirection(directionToEnemy);
                }
            }

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
        
        private Transform GetClosestEnemy()
        {
            try
            {
                var inGameManager = StaticServiceLocator.Instance.Get<InGameManager>();
                if (inGameManager?.PlayerDataDic == null || Parameter.Owner == null) return null;

                Transform closestEnemy = null;
                float closestDistance = float.MaxValue;
                Vector3 ownerPosition = Parameter.Owner.transform.position;

                foreach (var playerData in inGameManager.PlayerDataDic.Values)
                {
                    if (playerData == null || playerData == Parameter.Owner) continue;

                    var playerManager = playerData.GetComponent<PlayerManager>();
                    if (playerManager == null || playerManager.IsStun) continue;

                    float distance = Vector3.Distance(ownerPosition, playerData.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = playerData.transform;
                    }
                }

                return closestEnemy;
            }
            catch (System.Exception)
            {
                return null;
            }
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