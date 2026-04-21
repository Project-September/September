#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

public class PackageExporter : EditorWindow
{
    private const string TARGET_PATH = "Assets/AssetStoreTools";
    private const string LAST_TIME_KEY_PREFIX = "FolderDiff_LastExportTime_";

    private Vector2 _scrollPosition;
    private List<FolderStatus> _folderStatuses = new();

    private class FolderStatus
    {
        public string FolderName; // 孫フォルダ名
        public string ParentName; // 子フォルダ名（カテゴリ分け用）
        public string FullPath;
        public bool HasChanged;
        public DateTime LastModified;
        public DateTime LastExportTime;
    }

    [MenuItem("September/Package Exporter")]
    public static void ShowWindow()
    {
        GetWindow<PackageExporter>("Package Exporter");
    }

    private void OnEnable() => RefreshAnalysis();

    void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("各フォルダのエクスポート履歴に基づいて差分を表示しています", EditorStyles.miniLabel);
            if (GUILayout.Button("すべて更新済みとしてマーク", GUILayout.Width(160)))
            {
                if (EditorUtility.DisplayDialog("確認", "すべてのフォルダの更新日時を現在時刻に変更します。よろしいですか？", "はい", "いいえ"))
                {
                    foreach (var status in _folderStatuses)
                    {
                        SaveCurrentTime(status.FullPath);
                    }
                    RefreshAnalysis();
                }
            }
        }

        if (GUILayout.Button("差分を再スキャン", GUILayout.Height(30))) RefreshAnalysis();

        EditorGUILayout.Space();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        foreach (var status in _folderStatuses.ToList())
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea))
            {
                GUI.contentColor = status.HasChanged ? Color.yellow : Color.white;
                string icon = status.HasChanged ? "● [UPDATED] " : "○ ";

                // 「子フォルダ名 > 孫フォルダ名」の形式で表示
                EditorGUILayout.LabelField($"{icon}[{status.ParentName}] {status.FolderName}", GUILayout.Width(250));
                
                string lastExportStr = status.LastExportTime == DateTime.MinValue ? "未" : status.LastExportTime.ToString("yy/MM/dd HH:mm");
                EditorGUILayout.LabelField($"前: {lastExportStr}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"新: {status.LastModified:yy/MM/dd HH:mm}", GUILayout.Width(120));

                if (GUILayout.Button("開く", GUILayout.Width(40)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(status.FullPath);
                    EditorGUIUtility.PingObject(obj);
                }

                // 個別エクスポートボタン
                if (GUILayout.Button("Export", GUILayout.Width(50)))
                {
                    ExportSingleFolder(status);
                    GUIUtility.ExitGUI();
                }
                GUI.contentColor = Color.white;
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("更新があった孫フォルダをすべて書き出す", GUILayout.Height(40)))
        {
            ExportAllChangedFolders();
        }
    }

    private void RefreshAnalysis()
    {
        _folderStatuses.Clear();

        string rootFullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", TARGET_PATH));
        if (!Directory.Exists(rootFullPath)) return;

        // 1. 子フォルダを取得
        string[] childDirs = Directory.GetDirectories(rootFullPath);

        foreach (var child in childDirs)
        {
            string childName = Path.GetFileName(child);
            // 2. 孫フォルダを取得
            string[] grandchildDirs = Directory.GetDirectories(child);

            foreach (var grandchild in grandchildDirs)
            {
                string unityPath = ToUnityPath(grandchild);
                DateTime lastExportTime = GetLastExportTime(unityPath);

                var files = Directory.GetFiles(grandchild, "*.*", SearchOption.AllDirectories);
                DateTime maxModified = files.Length > 0
                    ? files.Max(f => File.GetLastWriteTime(f))
                    : Directory.GetLastWriteTime(grandchild);

                _folderStatuses.Add(new FolderStatus
                {
                    FolderName = Path.GetFileName(grandchild),
                    ParentName = childName,
                    FullPath = unityPath,
                    HasChanged = maxModified > lastExportTime,
                    LastModified = maxModified,
                    LastExportTime = lastExportTime
                });
            }
        }
        _folderStatuses = _folderStatuses.OrderByDescending(s => s.HasChanged).ThenBy(s => s.FolderName).ToList();
    }

    private void ExportSingleFolder(FolderStatus status)
    {
        string defaultName = $"{status.FolderName}.unitypackage";
        string exportPath = EditorUtility.SaveFilePanel("Export Grandchild Package", "", defaultName, "unitypackage");

        if (!string.IsNullOrEmpty(exportPath))
        {
            AssetDatabase.ExportPackage(status.FullPath, exportPath, ExportPackageOptions.Recurse);
            Debug.Log($"Exported: {exportPath}");

            SaveCurrentTime(status.FullPath);
            RefreshAnalysis();
        }
    }

    private void ExportAllChangedFolders()
    {
        var targets = _folderStatuses.Where(s => s.HasChanged).ToList();
        if (targets.Count == 0) return;

        string folderPath = EditorUtility.OpenFolderPanel("保存先フォルダを選択", "", "");
        if (string.IsNullOrEmpty(folderPath)) return;

        foreach (var target in targets)
        {
            string fileName = Path.Combine(folderPath, $"{target.FolderName}.unitypackage");
            AssetDatabase.ExportPackage(target.FullPath, fileName, ExportPackageOptions.Recurse);
            Debug.Log($"Auto Exported: {fileName}");
            SaveCurrentTime(target.FullPath);
        }
        
        RefreshAnalysis();
        EditorUtility.DisplayDialog("完了", $"{targets.Count}個のパッケージを書き出しました", "OK");
    }

    private DateTime GetLastExportTime(string path)
    {
        string key = GetKey(path);
        string ticks = EditorPrefs.GetString(key, "");
        return long.TryParse(ticks, out long result) ? DateTime.FromBinary(result) : DateTime.MinValue;
    }

    private void SaveCurrentTime(string path)
    {
        string key = GetKey(path);
        EditorPrefs.SetString(key, DateTime.Now.ToBinary().ToString());
    }
    
    private string GetKey(string path) => LAST_TIME_KEY_PREFIX + path;

    private string ToUnityPath(string fullPath)
    {
        fullPath = fullPath.Replace("\\", "/");
        int index = fullPath.IndexOf("Assets/");
        return index >= 0 ? fullPath.Substring(index) : null;
    }
}
#endif
