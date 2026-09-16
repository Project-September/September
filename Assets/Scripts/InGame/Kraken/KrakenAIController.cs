using System;
using UnityEngine;
using UnityEngine.AI;

namespace September.InGame.Kraken
{
    [Serializable]
    public class KrakenAIController
    {
        [Header("配置")]
        [Tooltip("未設定ならクラーケン自身の位置を中心にする")]
        [SerializeField] private Transform _aiAttackAreaCenter;
        [SerializeField] private float _attackRadius = 10f;

        [Header("タイミング")]
        [Tooltip("有効化されてから最初の攻撃を開始するまでの秒数")]
        [SerializeField] private float _attackStartDelay;
        [Tooltip("終了何秒前から攻撃をやめるか")]
        [SerializeField] private float _attackEndMargin;
        [SerializeField] private float _attackMinInterval = 3f;
        [SerializeField] private float _attackMaxInterval = 6f;

        private Transform _selfTransform;
        private float _activeDuration;
        private float _elapsedSinceActive;
        private float _nextAttackTime;
        private bool _isActive;

        public void Initialize(Transform selfTransform)
        {
            _selfTransform = selfTransform;
        }

        /// <summary> クラーケンが有効化された瞬間（Stayingに入った瞬間）に一度だけ呼ぶ </summary>
        /// <param name="activeDuration"> このAIが有効でいられる合計時間（＝Krakenの_stayDuration） </param>
        public void Begin(float activeDuration)
        {
            _activeDuration = activeDuration;
            _elapsedSinceActive = 0f;
            _nextAttackTime = _attackStartDelay;
            _isActive = true;
        }

        /// <summary> 搭乗された・退場したなど、AI攻撃を止めたい時に呼ぶ </summary>
        public void Stop()
        {
            _isActive = false;
        }
        
        /// <summary> 毎tick、Staying中かつRunner.IsForwardの時にKraken側から呼ぶ </summary>
        public void Tick(float deltaTime)
        {
            if (!_isActive) return;
            _elapsedSinceActive += deltaTime;
        }

        /// <summary> 攻撃可能か問い合わせるだけ。副作用なし </summary>
        public bool CanAttack()
        {
            if (!_isActive) return false;
            if (_elapsedSinceActive < _attackStartDelay) return false;
            if (_elapsedSinceActive > _activeDuration - _attackEndMargin) return false;
            return _elapsedSinceActive >= _nextAttackTime;
        }

        public Vector3 GetResolveNetwork()
        {
                Vector3 center = _aiAttackAreaCenter != null ? _aiAttackAreaCenter.position : _selfTransform.position;
                Vector2 offset = UnityEngine.Random.insideUnitCircle * _attackRadius;
                var randomPos = center + new Vector3(offset.x, 0f, offset.y);
                if (NavMesh.SamplePosition(
                        randomPos,
                        out NavMeshHit hit,
                        10,
                        NavMesh.AllAreas))
                {
                    Debug.Log($"AttackPos {randomPos} => {hit.position}");
                    return hit.position;
                }
                Debug.Log($"AttackPos {randomPos}");
                return randomPos;
        }

        /// <summary> 実際に攻撃した後、Kraken側から呼んでクールダウンを進める </summary>
        public void NotifyAttacked()
        {
            _nextAttackTime = _elapsedSinceActive + UnityEngine.Random.Range(_attackMinInterval, _attackMaxInterval);
        }
    }
}