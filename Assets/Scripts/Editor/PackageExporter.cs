using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

public class PackageExporter : EditorWindow
{
    private const string TARGET_PATH = "Assets/AssetStoreTools";
    private const string LAST_TIME_KEY = "FolderDiff_LastExportTime";

    private Vector2 _scrollPosition;
    private List<FolderStatus> _folderStatuses = new List<FolderStatus>();

    private class FolderStatus
    {
        public string FolderName; // 孫フォルダ名
        public string ParentName; // 子フォルダ名（カテゴリ分け用）
        public string FullPath;
        public bool HasChanged;
        public DateTime LastModified;
    }

    [MenuItem("September/Analyze Grandchild Diff")]
    public static void ShowWindow()
    {
        GetWindow<PackageExporter>("Package Exporter (Grandchild)");
    }

    private void OnEnable() => RefreshAnalysis();

    void OnGUI()
    {
        string lastTimeStr = GetLastExportTime() == DateTime.MinValue ? "記録なし" : GetLastExportTime().ToString("yyyy/MM/dd HH:mm:ss");
        
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField($"基準日時: {lastTimeStr}", EditorStyles.boldLabel);
            if (GUILayout.Button("基準日時を更新", GUILayout.Width(120)))
            {
                SaveCurrentTime();
                RefreshAnalysis();
            }
        }

        if (GUILayout.Button("差分を再スキャン", GUILayout.Height(30))) RefreshAnalysis();

        EditorGUILayout.Space();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        foreach (var status in _folderStatuses)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea))
            {
                GUI.contentColor = status.HasChanged ? Color.yellow : Color.white;
                string icon = status.HasChanged ? "● [UPDATED] " : "○ ";

                // 「子フォルダ名 > 孫フォルダ名」の形式で表示
                EditorGUILayout.LabelField($"{icon}[{status.ParentName}] {status.FolderName}", GUILayout.Width(250));
                EditorGUILayout.LabelField($"{status.LastModified:MM/dd HH:mm}", GUILayout.Width(100));

                if (GUILayout.Button("開く", GUILayout.Width(40)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(status.FullPath);
                    EditorGUIUtility.PingObject(obj);
                }

                // 個別エクスポートボタン
                if (GUILayout.Button("Export", GUILayout.Width(50)))
                {
                    ExportSingleFolder(status);
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
        DateTime lastTime = GetLastExportTime();

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
                var files = Directory.GetFiles(grandchild, "*.*", SearchOption.AllDirectories);
                DateTime maxModified = files.Length > 0
                    ? files.Max(f => File.GetLastWriteTime(f))
                    : Directory.GetLastWriteTime(grandchild);

                _folderStatuses.Add(new FolderStatus
                {
                    FolderName = Path.GetFileName(grandchild),
                    ParentName = childName,
                    FullPath = ToUnityPath(grandchild),
                    HasChanged = maxModified > lastTime,
                    LastModified = maxModified
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
        }
        
        SaveCurrentTime();
        RefreshAnalysis();
        EditorUtility.DisplayDialog("完了", $"{targets.Count}個のパッケージを書き出しました", "OK");
    }

    private DateTime GetLastExportTime()
    {
        string ticks = EditorPrefs.GetString(LAST_TIME_KEY, "");
        return long.TryParse(ticks, out long result) ? DateTime.FromBinary(result) : DateTime.MinValue;
    }

    private void SaveCurrentTime() => EditorPrefs.SetString(LAST_TIME_KEY, DateTime.Now.ToBinary().ToString());

    private string ToUnityPath(string fullPath)
    {
        fullPath = fullPath.Replace("\\", "/");
        int index = fullPath.IndexOf("Assets/");
        return index >= 0 ? fullPath.Substring(index) : null;
    }
}