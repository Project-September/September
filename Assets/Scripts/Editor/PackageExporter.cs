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
    private Vector2 _fileScrollPosition;
    private List<FolderStatus> _folderStatuses = new();
    private List<FileStatus> _changedFiles = new();

    private class FolderStatus
    {
        public string FolderName; // 孫フォルダ名
        public string ParentName; // 子フォルダ名（カテゴリ分け用）
        public string FullPath;
        public bool HasChanged;
        public DateTime LastModified;
        public DateTime LastExportTime;
    }

    private class FileStatus
    {
        public string RelativePath;
        public string FullPath;
        public DateTime LastModified;
        public bool HasUpdatedMeta;
        
        public const string MetaDisplayText = " [+META]";
        public string DisplayText => RelativePath + (HasUpdatedMeta ? MetaDisplayText : "");
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

            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(120));
            if (EditorGUI.DropdownButton(rect, new GUIContent("その他の操作"), FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("すべて更新済みとしてマーク"), false, MarkAllAsUpdated);
                menu.AddItem(new GUIContent("すべての孫フォルダを書き出す"), false, ExportAllFolders);
                menu.DropDown(rect);
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("差分を再スキャン", GUILayout.Height(30))) RefreshAnalysis();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("パッケージ状態", EditorStyles.boldLabel);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
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

        EditorGUILayout.LabelField($"更新があったファイル一覧 ({_changedFiles.Count}件)", EditorStyles.boldLabel);
        
        // パス表示に必要な幅を計算（表示されるファイルのみで計算）
        float maxPathWidth = _changedFiles.Count > 0 ? _changedFiles.Max(f => EditorStyles.label.CalcSize(new GUIContent(f.DisplayText)).x) : 0;
        
        _fileScrollPosition.y = EditorGUILayout.BeginScrollView(new Vector2(0, _fileScrollPosition.y), GUILayout.ExpandHeight(true)).y;
        
        foreach (var file in _changedFiles)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file.FullPath);
            var sourceAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file.FullPath.Replace(".meta", ""));
            bool isAsset = asset != null;
            bool isMetaFile = sourceAsset != null;
            bool isValidFile = isMetaFile || isAsset;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea))
            {
                // 更新日時
                EditorGUILayout.LabelField(file.LastModified.ToString("yy/MM/dd HH:mm"), GUILayout.Width(100));
                if (!isAsset)
                {
                    GUI.contentColor = isMetaFile ? Color.gray : Color.yellow;
                }

                // ファイルパス表示
                Rect pathAreaRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.BeginGroup(pathAreaRect);
                    Rect labelRect = new Rect(-_fileScrollPosition.x, 0, maxPathWidth + 20, pathAreaRect.height);
                    GUI.Label(labelRect, file.DisplayText);
                    GUI.EndGroup();
                }

                // ファイルを開く。または削除済み表示
                if (!isValidFile)
                {
                    GUI.contentColor = Color.yellow;
                    EditorGUILayout.LabelField("削除済み", GUILayout.Width(60));
                    GUI.contentColor = Color.white;
                }
                else
                {
                    GUI.contentColor = Color.white;
                    var rect = EditorGUILayout.GetControlRect(GUILayout.Width(40));
                    if (GUI.Button(rect, "開く"))
                    {
                        EditorGUIUtility.PingObject(isAsset ? asset : sourceAsset);
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();

        // 下部に水平スクロールバーを表示（パス表示エリアのスクロールを制御）
        // おおよその表示可能幅を計算
        float visiblePathWidth = position.width - 180;
        if (maxPathWidth > visiblePathWidth)
        {
            _fileScrollPosition.x = GUILayout.HorizontalScrollbar(_fileScrollPosition.x, visiblePathWidth, 0, maxPathWidth);
        }
        else
        {
            _fileScrollPosition.x = 0;
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("更新があった孫フォルダをすべて書き出す", GUILayout.Height(40)))
        {
            ExportAllChangedFolders();
        }
    }

    private void RefreshAnalysis()
    {
        _folderStatuses.Clear();
        _changedFiles.Clear();

        string rootFullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", TARGET_PATH));
        if (!Directory.Exists(rootFullPath)) return;

        List<FileStatus> allTempChanged = new();

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

                DateTime maxModified = files.Length == 0 ? Directory.GetLastWriteTime(grandchild) : DateTime.MinValue;

                foreach (var file in files)
                {
                    // 全てのファイルの中から直近の更新日時を見つける
                    DateTime modTime = File.GetLastWriteTime(file);
                    if (modTime > maxModified) maxModified = modTime;

                    // 前回のエクスポートから更新のあったファイルを見つけて一時リストに保存
                    if (modTime > lastExportTime)
                    {
                        allTempChanged.Add(new FileStatus
                        {
                            RelativePath = Path.GetRelativePath(child, file).Replace("\\", "/"),
                            FullPath = ToUnityPath(file),
                            LastModified = modTime
                        });
                    }
                }

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

        // メタファイルの重複フィルタリングと「+META」フラグの設定
        var changedPaths = new HashSet<string>(allTempChanged.Select(f => f.FullPath));
        foreach (var file in allTempChanged)
        {
            if (file.FullPath != null && file.FullPath.EndsWith(".meta"))
            {
                string assetPath = file.FullPath.Substring(0, file.FullPath.Length - 5);
                // 対応するアセットも更新されている場合は、メタファイル自体は表示しない
                if (changedPaths.Contains(assetPath)) continue;
            }

            // 表示するファイルに対して、対応するメタファイルも更新されているかチェック
            file.HasUpdatedMeta = changedPaths.Contains(file.FullPath + ".meta");
            _changedFiles.Add(file);
        }

        _folderStatuses = _folderStatuses.OrderByDescending(s => s.HasChanged).ThenBy(s => s.FolderName).ToList();
        _changedFiles = _changedFiles.OrderByDescending(f => f.LastModified).ToList();
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

    private void MarkAllAsUpdated()
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

    private void ExportAllChangedFolders()
    {
        var targets = _folderStatuses.Where(s => s.HasChanged).ToList();
        ExportFolders(targets);
    }

    private void ExportAllFolders()
    {
        ExportFolders(_folderStatuses);
    }

    private void ExportFolders(List<FolderStatus> targets)
    {
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
