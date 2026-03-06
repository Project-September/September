using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;


namespace InGame.Common.AnimationMontage
{
    /// <summary> 特定アセットパス以下の AnimationMontage を MontageRegistryDatabase に格納する </summary>
    public sealed class MontageRegistryBuilder : AssetPostprocessor, IPreprocessBuildWithReport
    {
        private const string RootFolder = "Assets/ScriptableObjects/AnimationMontage";
        private const string AddressableMontageGroupName = "AnimationMontage";
        private const string MontageRegistryDatabasePath = "Assets/ScriptableObjects/AnimationMontageDatabase.asset";
        private static bool _isRebuilding;

        public int callbackOrder => 0;

        /// <summary> RootFolder 以下の Montage が操作されたら RebuildAll </summary>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!IsInsideRoot(new [] { importedAssets, deletedAssets, movedAssets, movedFromAssetPaths })) return;
            
            if (!_isRebuilding)
            {
                try
                {
                    RebuildAll();
                }
                finally
                {
                    EditorApplication.delayCall += () => _isRebuilding = false;
                }
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            RebuildAll();
        }

        /// <summary> path が root 下にあるか </summary>
        static bool IsInsideRoot(string[][] lists)
        {
            foreach (var list in lists)
            {
                if (list.Any(path => path.StartsWith(RootFolder, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            
            return false;
        }

        [MenuItem("Tools/Montage Registry/Rebuild All")]
        public static void RebuildAll()
        {
            _isRebuilding = true;
            
            // フォルダ配下の該当 SO を列挙
            var guids = AssetDatabase.FindAssets($"t:{nameof(AnimationMontage)}", new[] { RootFolder });
            var entries = new List<(ulong id, string path)>(guids.Length);

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(AddressableMontageGroupName) ?? settings.CreateGroup(AddressableMontageGroupName, false, false, true,
                null, typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema));
            
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationMontage)) as AnimationMontage;
                if (asset == null) continue;
            
                // 安定IDを GUID から生成
                var id = Fnv1A64(guid);
            
                // アセットに埋め込んでおく
                if (asset.Id != id)
                {
                    asset.EditorSetId(id);
                    EditorUtility.SetDirty(asset);
                }
            
                // Addressable 化（アドレスは GUID に統一）
                var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    entry = settings.CreateOrMoveEntry(guid, group, false, false);
                    entry.address = guid;
                    EditorUtility.SetDirty(settings);
                }
                
                entries.Add((id, path));
            }
            
            // DB アセットを生成/更新
            EnsureDatabaseAsset(entries);
            
            AssetDatabase.SaveAssets();
            Debug.Log($"MontageRegistry Rebuilt.");
        }
        
        static void EnsureDatabaseAsset(List<(ulong id, string path)> entries)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MontageRegistryDatabasePath) ?? string.Empty);
            var existing = AssetDatabase.LoadAssetAtPath(MontageRegistryDatabasePath, typeof(ScriptableObject)) as MontageRegistryDatabase;

            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance(typeof(MontageRegistryDatabase)) as MontageRegistryDatabase;
                AssetDatabase.CreateAsset(existing, MontageRegistryDatabasePath);
            }

            // List<Entry> を構築
            var list = new List<MontageRegistry.Entry>();
            foreach (var (id, path) in entries)
            {
                var newEntry = new MontageRegistry.Entry()
                {
                    Id = id,
                    Reference = Activator.CreateInstance(typeof(AssetReferenceT<AnimationMontage>), AssetDatabase.AssetPathToGUID(path)) as AssetReferenceT<AnimationMontage>
                };
                
                list.Add(newEntry);
            }

            if (existing != null)
            {
                existing.Entries = list;
                EditorUtility.SetDirty(existing);
            }

            // DB 自体も Addressable にしておく（既に入ってるなら放置）
            var dbGuid = AssetDatabase.AssetPathToGUID(MontageRegistryDatabasePath);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(AddressableMontageGroupName) ?? settings.CreateGroup(AddressableMontageGroupName, false, false, true, null, typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema));
            var dbEntry = settings.FindAssetEntry(dbGuid) ?? settings.CreateOrMoveEntry(dbGuid, group);
            dbEntry.address = nameof(MontageRegistryDatabase);
            EditorUtility.SetDirty(settings);
        }
        
        static ulong Fnv1A64(string s)
        {
            const ulong offset = 1469598103934665603UL;
            const ulong prime = 1099511628211UL;
            ulong h = offset;
            s = s.Replace("-", "").ToLowerInvariant();
            var bytes = System.Text.Encoding.ASCII.GetBytes(s);
            unchecked
            {
                foreach (var t in bytes)
                {
                    h ^= t;
                    h *= prime;
                }
            }
            return h;
        }
    }
}
#endif
