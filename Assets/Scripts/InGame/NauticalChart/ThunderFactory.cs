using Cysharp.Threading.Tasks;
using Fusion;
using September;
using September.Common;
using September.InGame.Effect;
using System;
using System.Threading;
using UnityEngine;

public class ThunderFactory : NetworkBehaviour
{
    [Header("Prefab参照")]
    [SerializeField] private BoxCollider _spawnArea;
    [SerializeField] private SkyboxChanger _skyboxChanger;

    [Header("時間")]
    [SerializeField] private float _thunderLifeTime = 2.5f;
    [SerializeField] private float _thunderSpawnIntervalTime = 2.0f;

    [Header("個数制限")]
    private int _thunderSpawnCapacity = 5;

    private EffectSpawner _effectSpawner;
    CancellationTokenSource _cts;

    [Networked] public float Timer { get; private set; }

    public override void Spawned()
    {
        base.Spawned();
        _cts = new CancellationTokenSource();
        _effectSpawner ??= StaticServiceLocator.Instance.Get<EffectSpawner>();
    }

    /// <summary> 雷の生成処理 </summary>
    public void ThunderSpawener()
    {
        for (int i = 0; i < _thunderSpawnCapacity; i++)
        {
            var id = GenerateEffectId();
            _effectSpawner.RequestPlayLoopEffect
               (id, EffectType.Thunder, SpawnTransform(), Quaternion.identity, transform);
            ThunderDestroyAsync(id).Forget();
        }

        while( _skyboxChanger.changeSkyTime - _thunderLifeTime >= Timer)
        {
            Timer += Runner.DeltaTime;
            ThunderAsync().Forget();
        }
    }

    /// <summary> 雷の生成処理を非同期で行う </summary>
    private async UniTaskVoid ThunderAsync()
    {
        await UniTask.WaitForSeconds(_thunderSpawnIntervalTime, cancellationToken: _cts.Token);

        for (int i = 0; i < _thunderSpawnCapacity; i++)
        {
            var id = GenerateEffectId();
            _effectSpawner.RequestPlayLoopEffect
               (id, EffectType.Thunder, SpawnTransform(), Quaternion.identity, transform);
            ThunderDestroyAsync(id).Forget();
        }
    }

    /// <summary> 雷のエフェクトを一定時間後に停止する非同期処理 </summary>
    /// <param name="effectId"> 停止するエフェクトのID </param>
    private async UniTaskVoid ThunderDestroyAsync(string effectId)
    {
        await UniTask.WaitForSeconds(_thunderLifeTime, cancellationToken: _cts.Token);
        _effectSpawner.StopEffect(effectId);
    }

    /// <summary> エフェクトIDを生成する </summary>
    private static string GenerateEffectId()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary> 雷の生成位置をランダムに決定する </summary>
    private Vector3 SpawnTransform()
    {
       return new Vector3
            (UnityEngine.Random.Range(_spawnArea.bounds.min.x, _spawnArea.bounds.max.x),
             _spawnArea.bounds.max.y,
             UnityEngine.Random.Range(_spawnArea.bounds.min.z, _spawnArea.bounds.max.z));
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        _cts.Cancel();
        _cts.Dispose();
    }
}
