// Assets/Editor/RigHumanoidBatchPredefined.cs
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class RigHumanoidBatchPredefined
{
    // 対象フォルダ（必要ならここに追記）
    private static readonly string[] TARGET_FOLDERS = new[]
    {
        "Assets/Model/HARU",
        "Assets/Model/OKB",
        "Assets/Model/TANIHIRA",
        "Assets/Model/KOINUMA",
    };

    [MenuItem("Tools/Rig/Convert Predefined Folders to Humanoid")]
    private static void ConvertPredefinedFolders()
    {
        // Assets配下に存在するフォルダだけに絞る
        var validFolders = Array.FindAll(TARGET_FOLDERS, AssetDatabase.IsValidFolder);
        if (validFolders.Length == 0)
        {
            Debug.LogWarning("[RigHumanoidBatch] 有効なフォルダが見つかりません。設定を確認してください。");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Model", validFolders); // 再帰検索
        int changed = 0, skipped = 0, failed = 0, totalFbx = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) { skipped++; continue; }
                totalFbx++;

                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) { skipped++; continue; }

                bool needReimport = false;

                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    needReimport = true;
                }

#if UNITY_2018_1_OR_NEWER
                if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    needReimport = true;
                }
#endif

                try
                {
                    if (needReimport)
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        changed++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[RigHumanoidBatch] Failed: {path}\n{e}");
                    failed++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        // 存在しなかったフォルダの情報も出しておくと親切
        foreach (var f in TARGET_FOLDERS)
        {
            if (!AssetDatabase.IsValidFolder(f))
                Debug.LogWarning($"[RigHumanoidBatch] フォルダが見つかりません: {f}");
        }

        Debug.Log($"[RigHumanoidBatch] Done. FBX: {totalFbx}, Changed: {changed}, Skipped: {skipped}, Failed: {failed}");
    }
}
#endif
