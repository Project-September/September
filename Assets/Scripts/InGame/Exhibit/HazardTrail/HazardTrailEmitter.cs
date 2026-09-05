using Fusion;
using UnityEngine;

namespace InGame.Exhibit.HazardTrail
{
    /// <summary>
    /// 追従対象の移動軌跡（足元）に沿って、一定距離・一定時間間隔ごとに地面ハザードオブジェクトをスポーンさせるエミッター。
    /// </summary>
    public class HazardTrailEmitter : NetworkBehaviour
    {
        [Header("ハザード生成設定")]
        [SerializeField] private NetworkPrefabRef _hazardPrefab;
        [SerializeField] private float _distanceInterval = 1.0f;
        [SerializeField] private float _minTimeInterval = 0.2f;
        [SerializeField] private Vector3 _spawnOffset = Vector3.zero;

        private Transform _targetTransform;
        private PlayerRef _currentOwner;
        private Vector3 _lastSpawnPos;
        private TickTimer _intervalTimer;
        private bool _isEmitting;


        public void StartEmitting(Transform targetTransform, PlayerRef owner)
        {
            _targetTransform = targetTransform;
            _currentOwner = owner;
            _lastSpawnPos = targetTransform.position;
            _intervalTimer = TickTimer.CreateFromSeconds(Runner, _minTimeInterval);
            _isEmitting = true;
        }


        public void StopEmitting()
        {
            _isEmitting = false;
            _targetTransform = null;
            _currentOwner = PlayerRef.None;
        }

        public void UpdateEmitter()
        {
            if (!_isEmitting || _targetTransform == null || !HasStateAuthority) return;
            float dist = Vector3.Distance(_targetTransform.position, _lastSpawnPos);
            bool timePassed = _intervalTimer.ExpiredOrNotRunning(Runner);

            if (dist >= _distanceInterval && timePassed)
            {
                SpawnHazard(_targetTransform.position + _spawnOffset);
                _lastSpawnPos = _targetTransform.position;
                _intervalTimer = TickTimer.CreateFromSeconds(Runner, _minTimeInterval);
            }
        }
        private void SpawnHazard(Vector3 position)
        {
            var netObj = Runner.Spawn(_hazardPrefab, position, Quaternion.identity, _currentOwner);
            if (netObj != null && netObj.TryGetComponent<GroundHazard>(out var hazard))
            {
                hazard.Initialize(_currentOwner, position);
            }
        }
    }
}
