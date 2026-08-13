using System;
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
        [SerializeField] private Transform[] _spawnPositions;
        [SerializeField] private GameTimerData _timerData;
        [SerializeField] private Transform _spawnPredictionRange;
        [SerializeField] private float _predictionVisibleDuration;
        [SerializeField] private JewelrySpawnData _jewelrySpawnData;

        private int _nextTime = 0;

        private CancellationTokenSource _cts;

        public override void Spawned()
        {
            if (!HasStateAuthority)
                return;

            Array.Sort(_jewelrySpawnData.SpawnSettings, (a, b) => b.SpawnTime.CompareTo(a.SpawnTime));

            WaitSpawnAsync().Forget();
        }

        private async UniTask WaitSpawnAsync()
        {
            _cts = new CancellationTokenSource();
            _nextTime = 0;

            await WaitForSpawnTimingAsync();
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

                if (_nextTime >= _jewelrySpawnData.SpawnSettings.Length)
                    return;

                if (seconds <= _jewelrySpawnData.SpawnSettings[_nextTime].SpawnTime)
                {
                    StartSpawnSequenceAsync(_jewelrySpawnData.SpawnSettings[_nextTime]).Forget();
                    _nextTime++;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, _cts.Token);
            }
        }

        private async UniTask StartSpawnSequenceAsync(JewelrySpawnSetting spawnSetting)
        {
            if (_spawnPositions == null || _spawnPositions.Length == 0)
            {
                Debug.LogError("JewelrySpawner : スポーン位置が設定されていません。");
                return;
            }

            Transform spawnTransform;

            if (spawnSetting.PositionIndex < 0)
            {
                int randomIndex = Random.Range(0, _spawnPositions.Length);
                Transform randomTransform = _spawnPositions[randomIndex];
                spawnTransform = randomTransform;
            }
            else
            {
                if (spawnSetting.PositionIndex >= 0 && spawnSetting.PositionIndex < _spawnPositions.Length)
                {
                    spawnTransform = _spawnPositions[spawnSetting.PositionIndex];
                }
                else
                {
                    Debug.LogError("JewelrySpawner : 存在しないインデックス番号です");
                    return;
                }
            }
            RPC_SetSpawnPrediction(true, spawnTransform.position);

            //スポーン予告メッセージを出す
            if (spawnSetting.ShowSpawnMessage)
                RPC_ShowSpawnMessage(_predictionVisibleDuration);

            await UniTask.WaitForSeconds(_predictionVisibleDuration, cancellationToken: _cts.Token);
            SpawnJewelryGroup(spawnTransform.position, spawnSetting);
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

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShowSpawnMessage(float second)
        {
            UIController.I.ShowStatusUpUI(second, Exhibit.StatusUpType.JewelrySpawn);
        }

        private void SpawnJewelryGroup(Vector3 centerPosition, JewelrySpawnSetting spawnSetting)
        {
            centerPosition.y += spawnSetting.Height;

            for (int i = 0; i < spawnSetting.Items.Length; i++)
            {
                int count = spawnSetting.Items[i].Count;
                for (int j = 0; j < count; j++)
                {
                    Vector2 randomOffset = Random.insideUnitCircle * spawnSetting.SpawnRange;

                    Vector3 spawnPosition = centerPosition;
                    spawnPosition.x += randomOffset.x;
                    spawnPosition.z += randomOffset.y;

                    var prefab = _jewelrySpawnData.GetPrefab(spawnSetting.Items[i].JewelryType);

                    if (prefab == null)
                    {
                        Debug.LogError($"JewelrySpawner : 宝石のPrefabが設定されていません。宝石の種類: {spawnSetting.Items[i].JewelryType}");
                        continue;
                    }

                    Runner.Spawn(prefab, spawnPosition, Quaternion.identity);
                }
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
