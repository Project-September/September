using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using September.InGame.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace InGame.Jewelry
{
    public class JewelrySpawner : NetworkBehaviour
    {
        [SerializeField] private Transform[] _randomSpawnPositions;
        [SerializeField] private GameTimerData _timerData;
        [SerializeField] private float _spawnRange;
        [SerializeField] private int _spawnCount;
        [SerializeField] private NetworkObject _jewelryPrefab;
        [SerializeField] private Transform _spawnPredictionRange;
        [SerializeField] private float _predictionVisibleDuration;
        [SerializeField] private float _spawnTime;

        private CancellationTokenSource _cts;

        public override void Spawned()
        {
            if (!HasStateAuthority)
                return;

            WaitSpawnAsync().Forget();
        }

        private async UniTask WaitSpawnAsync()
        {
            _cts = new CancellationTokenSource();

            await WaitForSpawnTimingAsync();
            await StartSpawnSequenceAsync();
        }

        private async UniTask WaitForSpawnTimingAsync()
        {
            int tickRate = Runner.TickRate;
            int gameEndTick = Runner.Tick + Mathf.RoundToInt((_timerData.GameTime + _timerData.PreStartTime) * tickRate);

            int lastTick = Runner.Tick;

            while (Runner.Tick < gameEndTick)
            {
                if (_cts.IsCancellationRequested)
                    return;

                if (Runner.Tick == lastTick)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, _cts.Token);
                    continue;
                }

                lastTick = Runner.Tick;

                int remaining = gameEndTick - Runner.Tick;
                int seconds = Mathf.CeilToInt(remaining / (float)tickRate);
                if (seconds <= _spawnTime)
                {
                    return;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, _cts.Token);
            }
        }

        private async UniTask StartSpawnSequenceAsync()
        {
            if (_randomSpawnPositions == null || _randomSpawnPositions.Length == 0)
            {
                Debug.LogError("JewelrySpawner : スポーン位置が設定されていません。");
                return;
            }

            Vector3 randomPosition = _randomSpawnPositions[Random.Range(0, _randomSpawnPositions.Length)].position;

            RPC_SetSpawnPrediction(true,randomPosition);

            await UniTask.WaitForSeconds(_predictionVisibleDuration, cancellationToken: _cts.Token);
            SpawnJewelryGroup(randomPosition);
            RPC_SetSpawnPrediction(false);
        }


        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetSpawnPrediction(bool visible, Vector3 position = default)
        {
            _spawnPredictionRange.gameObject.SetActive(visible);

            if (visible)
            {
                _spawnPredictionRange.position = position;
            }
        }

        private void SpawnJewelryGroup(Vector3 centerPosition)
        {
            for (int i = 0; i < _spawnCount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * _spawnRange;

                Vector3 spawnPosition = centerPosition;
                spawnPosition.x += randomOffset.x;
                spawnPosition.z += randomOffset.y;

                Runner.Spawn(_jewelryPrefab, spawnPosition, Quaternion.identity);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}