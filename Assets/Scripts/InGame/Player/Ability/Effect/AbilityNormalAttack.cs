using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Common;
using InGame.Health;
using InGame.Player;
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
        [SerializeField] protected int _attackDamage = 10;
        [SerializeField] private int _startHitCheckFrame = 17;
        [SerializeField] private int _endHitCheckFrame   = 21;
        [SerializeField] private int _endAttackFrame     = 22;
        [SerializeField] private bool _isStopWhenAttack = true;
        [SerializeField] private float _searchRadius = 2f;
        [SerializeField] protected EffectType _hitEffect = EffectType.HitNormal;
        [SerializeField] private AnimationClipPlayer _animationClipPlayer;
        
        [Header("自動エイム設定")]
        [SerializeField] private bool _enableAutoAim = true;
        [SerializeField] private float _moveForwardSpeed = 2f;
        
        [Header("Hit Box Cast")]
        [SerializeField] private Vector3 _boxHalfExtents = new Vector3(0.45f, 0.85f, 0.45f);
        [SerializeField] private Vector3 _boxLocalOffset = new Vector3(0f, 0.9f, 0.6f);
        [SerializeField, Tooltip("前方へ掃引する距離")]
        private float _boxCastDistance = 1.0f;
        [SerializeField] private LayerMask _hitLayer = ~0;
        [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];
        private readonly HashSet<Collider> _alreadyHit = new HashSet<Collider>();
        
        [Header("Debug Draw")]
        [SerializeField] private Color _debugHitBoxColor   = new Color(0f, 0.6f, 1f, 1f);
        [SerializeField, Tooltip("Debug 線の表示秒数。0 なら 1 フレームだけ")]
        private float _debugDrawDuration = 1f; // 例: 0.05f

        // 変換後のTickオフセット
        int _startHitTick, _endHitTick, _endAttackTick;

        // 攻撃開始Tick
        int _attackStartTick = -1;
        
        // 最も近い敵のTransform
        private Transform _closestEnemyTransform;
        private PlayerMovement _playerMovement;
        protected EffectSpawner _effectSpawner;

        protected override void OnStart()
        {
            if (_animationClipPlayer&& _normalAttackAnimationClip)
            {
                _animationClipPlayer.PlayClip(_normalAttackAnimationClip);
            }
            
            if (!_effectSpawner)
                _effectSpawner = StaticServiceLocator.Instance.Get<EffectSpawner>();
            
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
            
            _startHitTick  = FrameToTick(_startHitCheckFrame);
            
        }

        public override void OnUpdateLocal(float deltaTime, GameObject owner)
        {
            if (HitboxDebugUtility.IsDebugModeEnabled)
            {
                var t = owner.transform;
                if (!t) return;

                // BoxCast の原点と向き
                var origin = t.position + t.TransformVector(_boxLocalOffset);
                var dir    = t.forward;
                var rot    = t.rotation;
                
                if (HitboxDebugUtility.IsDebugModeEnabled)
                {
                    // 表示したい位置を1つだけ選ぶ
                    // BoxCast なら「終了位置だけ」見せるのが直感的
                    var boxCenter = _boxCastDistance > 0f
                        ? origin + dir.normalized * _boxCastDistance   // 終了側
                        : origin;                                       // 距離0なら開始側

                    HitboxDebugUtility.DrawBoxOneFrame(
                        boxCenter,
                        _boxHalfExtents,
                        rot,
                        _debugHitBoxColor   // 好きな色に
                    );
                }

            }

        }

        protected virtual void OnHitEnemy(Collider hitInfo, Vector3 hitPosition)
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

            //エフェクトの再生
            _effectSpawner.RequestPlayOneShotEffect(_hitEffect, hitInfo.ClosestPoint(hitInfo.bounds.ClosestPoint(hitPosition)), Quaternion.identity);
        }
        
        private void CastAndApplyHits()
        {
            var t = Parameter.Owner.transform;

            // BoxCast の原点と向き
            var origin = t.position + t.TransformVector(_boxLocalOffset);
            var dir    = t.forward;
            var rot    = t.rotation;

            // 掃引（NonAlloc で GC しない）
            int hitCount = Physics.BoxCastNonAlloc(
                origin,
                _boxHalfExtents,
                dir,
                _hitBuffer,
                rot,
                _boxCastDistance,
                _hitLayer,
                _triggerInteraction
            );

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hitBuffer[i];
                var col = hit.collider;
                if (col == null) continue;

                // 自分自身除外
                if (col.GetComponentInParent<NetworkObject>() == Parameter.Owner) continue;

                // 二度当たり防止
                if (_alreadyHit.Contains(col)) continue;
                _alreadyHit.Add(col);

                // ヒット位置が 0 のことがあるのでフォールバック
                var hitPos = hit.point;
                if (hitPos == Vector3.zero)
                    hitPos = origin + dir * Mathf.Max(0.1f, _boxCastDistance * 0.5f);

                OnHitEnemy(col, hitPos);
            }
            // バッファ初期化（念のため）
            Array.Clear(_hitBuffer, 0, hitCount);
            
            if (_isStopWhenAttack && _playerMovement)
            {
                _playerMovement.IgnoreInput = true;
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
        
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

            if (inWindow)
            {
                CastAndApplyHits(); // ← ここで BoxCast 実行
            }
            else
            {
                // 窓を抜けたら次の攻撃に備えてクリア
                if (elapsed >= _endHitTick && _alreadyHit.Count > 0)
                    _alreadyHit.Clear();
            }
            
            _playerMovement.AddForce(Parameter.Owner.transform.forward * _moveForwardSpeed);

            // 攻撃終了
            if (elapsed >= _endAttackTick)
            {
                if (_isStopWhenAttack && _playerMovement)
                {
                    _playerMovement.IgnoreInput = false;
                }
                _phase = AbilityPhase.Ending;
            }
        }
        
        private Transform GetClosestEnemy()
        {
            try
            {
                var inGameManager = StaticServiceLocator.Instance.Get<InGameManager>();
                if (inGameManager?.PlayerDataDic == null || Parameter.Owner == null) return null;

                Transform closestEnemy = null;
                float closestDistance = _searchRadius;
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
    }
}

