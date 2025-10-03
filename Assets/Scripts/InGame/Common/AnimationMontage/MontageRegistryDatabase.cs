using System;
using System.Collections.Generic;
using System.Threading;
using Common.Attribute;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace InGame.Common.AnimationMontage
{
    public class MontageRegistryDatabase : ScriptableObject
    {
        [SerializeField, NonReorderable] public List<MontageRegistry.Entry> Entries = new();
    }
    
    public static class MontageRegistry
    {
        private const string DatabaseAddress = "MontageRegistryDatabase";

        [Serializable]
        public struct Entry
        {
            [SerializeField, ReadOnly] public ulong Id;
            [SerializeField, ReadOnly] public AssetReferenceT<AnimationMontage> Reference;
        }

        static bool _initialized;
        static MontageRegistryDatabase _db;

        // 参照とハンドルをランタイム辞書で管理（シリアライズしない）
        static readonly Dictionary<ulong, AssetReferenceT<AnimationMontage>> _refs = new();
        static readonly Dictionary<ulong, AsyncOperationHandle<AnimationMontage>> _handles = new();

        static async UniTask InitializeAsync()
        {
            if (_initialized) return;

            await Addressables.InitializeAsync().Task;

            var dbHandle = Addressables.LoadAssetAsync<MontageRegistryDatabase>(DatabaseAddress);
            await dbHandle.Task;
            if (dbHandle.Status != AsyncOperationStatus.Succeeded || dbHandle.Result == null)
                throw new Exception($"Addressables: DB not found: '{DatabaseAddress}'");

            _db = dbHandle.Result;

            _refs.Clear();
            foreach (var e in _db.Entries)
                _refs[e.Id] = e.Reference;

            _initialized = true;
        }

        public static async UniTask<AnimationMontage> LoadAsync(ulong id, CancellationToken ct = default)
        {
            await InitializeAsync();

            if (!_refs.TryGetValue(id, out var aref))
                throw new KeyNotFoundException($"AnimationMontage id={id} not found in DB");

            // すでにロード済みならそのハンドルを返す
            if (_handles.TryGetValue(id, out var handle) && handle.IsValid())
                return await AwaitHandle(handle, ct);

            // 別経路でロード開始済みなら Convert して再利用
            handle = aref.OperationHandle.IsValid() ? aref.OperationHandle.Convert<AnimationMontage>() : aref.LoadAssetAsync();

            _handles[id] = handle;
            return await AwaitHandle(handle, ct);
        }

        static async UniTask<AnimationMontage> AwaitHandle(AsyncOperationHandle<AnimationMontage> h, CancellationToken ct)
        {
            await h.ToUniTask(cancellationToken: ct);
            if (h.Status != AsyncOperationStatus.Succeeded)
                throw h.OperationException;
            return h.Result;
        }
    }
}
