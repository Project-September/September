using Fusion;
using InGame.Health;
using September.Common;
using September.InGame.Effect;
using UnityEngine;
using System.Collections.Generic;

namespace InGame.Exhibit.HazardTrail
{
    /// <summary>
    /// 地面ハザードの一定範囲内にいる敵対対象を検知し、定期的なスリップダメージを与えるとともに
    /// 設置中のループ視覚エフェクトの再生・停止を管理します。
    /// </summary>
    public class DamageHazardEffect : MonoBehaviour, IHazardEffect
    {
        [Header("攻撃判定設定")]
        [SerializeField] private float _radius = 1.2f;
        [SerializeField] private Vector3 _offset = new Vector3(0, 0.5f, 0);
        [SerializeField] private LayerMask _targetLayer;

        [Header("ダメージ設定")]
        [SerializeField] private int _damage = 1;
        [SerializeField] private float _damageInterval = 1f;

        [Header("ハザード演出エフェクト")]
        [SerializeField] private bool _useHazardEffect = true;
        [SerializeField] private EffectType _hazardEffectType = EffectType.CandleAura;
        [SerializeField] private Vector3 _hazardEffectOffset = Vector3.zero;
        [SerializeField] private Vector3 _hazardEffectScale = Vector3.one;

        private TickTimer _attackTimer;
        private EffectSpawner EffectSpawner => StaticServiceLocator.Instance.Get<EffectSpawner>();
        private EffectID _activeEffectId;
        private bool _hasStartedEffect;

        private readonly Collider[] _hitColliders = new Collider[10];
        private readonly HashSet<IDamageable> _damagedTargets = new HashSet<IDamageable>();

        public void OnHazardSpawn(NetworkRunner runner, PlayerRef owner)
        {
            PlayHazardEffect();
        }

        public void OnHazardTick(NetworkRunner runner, PlayerRef owner)
        {
            // インターバルタイマーが切れた時だけ攻撃判定を行う
            if (!_attackTimer.ExpiredOrNotRunning(runner)) return;

            Vector3 center = transform.position + _offset;
            int hitCount = Physics.OverlapSphereNonAlloc(center, _radius, _hitColliders, _targetLayer);

            _damagedTargets.Clear();

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hitColliders[i];
                if (hit == null) continue;
                var root = hit.transform.root;
                if (!root.TryGetComponent<IDamageable>(out var damageable)) continue;
                if (damageable.OwnerPlayerRef == owner) continue;
                // 同一キャラの複数コライダーによる重複ヒットを防止
                if (_damagedTargets.Add(damageable))
                {
                    var hitData = new HitData(HitActionType.Damage, _damage, owner, damageable.OwnerPlayerRef);
                    damageable.TakeHit(ref hitData);
                }
            }

            // タイマー再設定
            _attackTimer = TickTimer.CreateFromSeconds(runner, _damageInterval);
        }

        public void OnHazardDespawn(NetworkRunner runner)
        {
            StopHazardEffect();
        }

        private void PlayHazardEffect()
        {
            if (!_useHazardEffect || _hasStartedEffect || EffectSpawner == null) return;
            _activeEffectId = EffectSpawner.RequestPlayLoopEffect(
            _hazardEffectType,
            transform.position + _hazardEffectOffset,
            Quaternion.identity,
            _hazardEffectScale
            );
            _hasStartedEffect = true;
        }

        private void StopHazardEffect()
        {
            if (_activeEffectId.IsValid && EffectSpawner != null)
            {
                EffectSpawner.StopEffect(_activeEffectId);
                _activeEffectId = default;
            }
        }

        private void OnDestroy()
        {
            StopHazardEffect();
        }

#if UNITY_EDITOR
        // エディタ上で判定範囲を可視化
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + _offset, _radius);
        }
#endif
    }
}
