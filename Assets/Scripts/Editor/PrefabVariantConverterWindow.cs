#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>任意の Prefab を指定したベースの Variant として同じパスに保存する。</summary>
public sealed class PrefabVariantConverterWindow : EditorWindow
{
    private const string MenuPath = "Tools/Prefabs/Convert to Prefab Variant";
    private const string AssetMenuPath = "Assets/Prefabs/Convert to Prefab Variant";
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private GameObject basePrefab;
    private string resultMessage;

    [MenuItem(MenuPath)]
    [MenuItem(AssetMenuPath)]
    public static void Open()
    {
        var window = GetWindow<PrefabVariantConverterWindow>("Prefab Variant Converter");
        window.minSize = new Vector2(440, 260);
        if (IsPrefabRoot(Selection.activeObject as GameObject))
            window.targetPrefab = (GameObject)Selection.activeObject;
        window.Show();
    }

    [MenuItem(AssetMenuPath, true)]
    private static bool CanOpenFromAsset() => IsPrefabRoot(Selection.activeObject as GameObject);

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Prefab を Variant に変換", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        targetPrefab = (GameObject)EditorGUILayout.ObjectField("変換対象 Prefab", targetPrefab, typeof(GameObject), false);
        basePrefab = (GameObject)EditorGUILayout.ObjectField("継承元 Prefab", basePrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
            resultMessage = null;

        EditorGUILayout.HelpBox("対象の値・追加オブジェクト・追加コンポーネントを Override として残します。" +
            "継承元にだけある要素は継承されます。バックアップ後、対象と同じパスに上書き保存します。" +
            "同名オブジェクトや同型コンポーネントが複数ある場合、変換後の参照を確認してください。", MessageType.Info);
        string error = Validate(targetPrefab, basePrefab);
        if (error != null)
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        using (new EditorGUI.DisabledScope(error != null))
        {
            if (GUILayout.Button("Variant として保存"))
            {
                try
                {
                    string backupPath = Convert(targetPrefab, basePrefab);
                    resultMessage = backupPath == null ? "既に指定した継承元の Variant です。" :
                        $"変換完了。バックアップ:\n{backupPath}";
                }
                catch (Exception exception)
                {
                    resultMessage = "変換失敗: " + exception.Message;
                    Debug.LogException(exception);
                }
            }
        }
        if (!string.IsNullOrEmpty(resultMessage))
            EditorGUILayout.HelpBox(resultMessage, MessageType.Info);
    }

    private static bool IsPrefabRoot(GameObject prefab)
    {
        return prefab != null && EditorUtility.IsPersistent(prefab) && PrefabUtility.IsPartOfPrefabAsset(prefab)
            && AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(prefab)) == prefab;
    }

    private static string Validate(GameObject target, GameObject source)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
            return "Play Mode を終了し、Prefab Mode を閉じてください。";
        if (!IsPrefabRoot(target) || !IsPrefabRoot(source))
            return "Project 内の Prefab アセットを変換対象と継承元に指定してください。";
        string targetPath = AssetDatabase.GetAssetPath(target);
        if (!targetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
            !targetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
            PrefabUtility.GetPrefabAssetType(target) == PrefabAssetType.Model)
            return "変換対象は Assets 内の編集可能な .prefab にしてください。モデルは継承元として指定できます。";
        if (target == source)
            return "変換対象と継承元には別の Prefab を指定してください。";
        if (!AssetDatabase.IsOpenForEdit(targetPath))
            return "変換対象の Prefab を編集可能にしてください。";
        return null;
    }

    // メニュー以外の Editor スクリプトからも利用可能。成功時にバックアップのパスを返す。
    public static string Convert(GameObject original, GameObject basePrefab)
    {
        string error = Validate(original, basePrefab);
        if (error != null)
            throw new InvalidOperationException(error);
        string targetPath = AssetDatabase.GetAssetPath(original);

        if (IsExpectedVariant(original, basePrefab))
            return null;

        // 循環する継承関係は作成できない。
        for (var ancestor = basePrefab; ancestor != null;
             ancestor = PrefabUtility.GetCorrespondingObjectFromSource(ancestor))
        {
            if (ancestor == original)
                throw new InvalidOperationException("継承関係が循環するため変換できません。");
        }

        // ネストされた Prefab や他のアセットを経由する依存関係も検査する。
        if (Array.IndexOf(AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(basePrefab), true), targetPath) >= 0)
            throw new InvalidOperationException("継承元が変換対象に依存しています。循環するため変換できません。");

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string fullPath = Path.Combine(projectRoot, targetPath);
        string backupDirectory = Path.Combine(projectRoot, "Library", "PrefabVariantBackups",
            DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(backupDirectory);
        string backupPath = Path.Combine(backupDirectory, Path.GetFileName(targetPath));
        File.Copy(fullPath, backupPath);
        File.Copy(fullPath + ".meta", backupPath + ".meta");
        string originalGuid = AssetDatabase.AssetPathToGUID(targetPath);

        // Preview Scene のルートは Prefab 編集対象と判定され、変換 API に拒否される。
        // 通常の空シーンを追加で開き、既存シーンとその未保存変更は保持する。
        var previousActiveScene = SceneManager.GetActiveScene();
        var workingScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        bool saveAttempted = false;
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(original, workingScene);
            // 既存 Variant の継承を根元まで解除する。子の Nested Prefab は維持する。
            while (PrefabUtility.IsPartOfPrefabInstance(instance))
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.OutermostRoot,
                    InteractionMode.AutomatedAction);

            // 階層内の名前で照合する。対象の値と追加要素を残し、
            // 継承元にのみ存在する要素はベースから継承する。
            // API: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ConvertToPrefabInstanceSettings.html
            var settings = new ConvertToPrefabInstanceSettings
            {
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                recordPropertyOverridesOfMatches = true,
                gameObjectsNotMatchedBecomesOverride = true,
                componentsNotMatchedBecomesOverride = true,
                changeRootNameToAssetName = false,
                logInfo = true
            };
            PrefabUtility.ConvertToPrefabInstance(instance, basePrefab, settings,
                InteractionMode.AutomatedAction);

            if (PrefabUtility.GetCorrespondingObjectFromSource(instance) != basePrefab)
                throw new InvalidOperationException("継承元への接続に失敗しました。保存を中止します。");

            saveAttempted = true;
            PrefabUtility.SaveAsPrefabAsset(instance, targetPath, out bool success);
            if (!success)
                throw new InvalidOperationException("Variant の保存に失敗しました。");

            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            if (!IsExpectedVariant(saved, basePrefab) || AssetDatabase.AssetPathToGUID(targetPath) != originalGuid)
                throw new InvalidOperationException("保存後の継承元または GUID の検証に失敗しました。");

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log($"{targetPath} を {basePrefab.name} の Variant として保存しました。\n" +
                      $"バックアップ: {backupPath}\n" +
                      "Inspector の Overrides とモデル・コンポーネント参照を確認してください。", saved);
            return backupPath;
        }
        catch
        {
            if (saveAttempted)
            {
                File.Copy(backupPath, fullPath, true);
                File.Copy(backupPath + ".meta", fullPath + ".meta", true);
                AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
            }
            Debug.LogError($"変換に失敗しました。バックアップ: {backupPath}");
            throw;
        }
        finally
        {
            // 成功・失敗のどちらでも作業用シーンだけ破棄する。
            try
            {
                if (workingScene.IsValid() && workingScene.isLoaded)
                    EditorSceneManager.CloseScene(workingScene, true);
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }
        }
    }

    private static bool IsExpectedVariant(GameObject prefab, GameObject basePrefab)
    {
        return prefab != null && PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant
                              && PrefabUtility.GetCorrespondingObjectFromSource(prefab) == basePrefab;
    }
}
#endif
