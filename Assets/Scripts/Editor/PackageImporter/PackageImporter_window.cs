using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Numerics;
using UnityEditor;
using UnityEngine;

 namespace September.Editor.PackageImporter
{
    public class PackageImporter_window : EditorWindow
    {
        private const string PrefApiKey = "GDPI_ApiKey";
        private const string PrefFolderId = "GDPI_FolderId";
        private const string PrefInteractive = "GDPI_Interactive";

        private static readonly Color UpdatedColor = new Color(1f, 0.92f, 0.35f); // 更新ありの色ダヨ
        private static readonly Color NormalColor = Color.White; // 最新の色ダヨ

        private string _apiKey = "";
        private string _folderId = "";
        private bool _interactiveImport;
        private bool _showSettings = true;
        
        private List<DriveFileInfo> _driveFiles = new List<DriveFileInfo>();
        private Vector2 _scrollPos;
        private bool _isBusy;
        private string _statusMessage = "";

        private readonly Dictionary<string, DriveFileInfo> _pendingImports = new Dictionary<string, DriveFileInfo>();
        private Queue<DriveFileInfo> _bulkQueue = new Queue<DriveFileInfo>();
        private bool _pendingBulkContinue;

        [MenuItem("September/Package Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<PackageImporterWindow>("PackageImporter");
            window.miniSize = new Vector2(480, 360);
        }

        private void OnEnable()
        {
            _webAppUrl = EditorPrefs.GetString(PrefWebAppUrl, "");
            _folderId = EditorPrefs.GetString(PrefFolderId, "");
            _interactiveImport = EditorPrefs.GetBool(PrefInteractive, false);

            PackageImportCache.Load();

            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
            AssetDatabase.importPackageFailed += OnImportPackageFailed;
            AssetDatabase.importPackageCompleted += OnImportPackageCancelled;
        }

        private void OnDisable()
        {
            AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;
            AssetDatabase.importPackageFailed -= OnImportPackageFailed;
            AssetDatabase.importPackageCompleted -= OnImportPackageCancelled;
        }

        private void OnGUI()
        {
            DrawSetting();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_isBusy || string.IsNullOrEmpty(_webAppUrl) || string.IsNullOrEmpty(_folderId)))
            {
                if (GUILayout.Button("リフレッシュ（更新確認）", GUILayout.Height(28)))
                {
                    RefreshList();
                }
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
            }

            EditorGUILayout.Space();
            DrawPackageList();

            EditorGUILayout.Space();

            bool anyUpdated = _driveFiles.Any(IsUpdated);
            using (new EditorGUI.DisabledScope(_isBusy || !anyUpdated))
            {
                if (GUILayout.Button("更新済みパッケージを一括Import", GUILayout.Height(32)))
                {
                    ImportAllUpdated();
                }
            }
        }

        private void DrawSettings()
        {
            _showSettings = EditorGUILayout.Foldout(_showSettings, "設定", true);
            if (!_showSettings) return;

            EditorGUI.BeginChangeCheck();
            _webAppUrl = EditorGUILayout.TextField("Apps Script Web App URL", _webAppUrl);
            _folderId = EditorGUILayout.TextField("Drive フォルダID", _folderId);
            _interactiveImport = EditorGUILayout.ToggleLeft("Import時にダイアログを表示する", _interactiveImport);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString(PrefWebAppUrl, _webAppUrl);
                EditorPrefs.SetString(PrefFolderId, _folderId);
                EditorPrefs.SetBool(PrefInteractive, _interactiveImport);
            }

            EditorGUI.HelpBox(
                "AppsScript/Code.gs をGoogle Apps Scriptプロジェクトとしてデプロイし、" + 
                "発行されたWebアプリのURLを入力して下さい（APIキーや請求先アカウントの設定は不要です",
                MessageType.None);
        }

        private void DrawPackageList()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));

            if (_driveFiles.Count == 0)
            {
                EditorGUILayout.LabelField("パッケージがありません　リフレッシュしてください");
            }

            foreach (var file in _driveFiles)
            {
                bool updated = IsUpdated(file);
                Color bgColor = updated ? UpdatedColor : NormalColor;

                Rect rowRect = EditorGUILayout.BeginVertical(GUILayout.MinHeight(60));
                if (EventArgs.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rowRect, bgColor);
                }

                GUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(6);

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(file.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"更新日時: {FormatDate(file.modifiedTime)} サイズ: {FormatSize(file.size)}");
                EditorGUILayout.LabelField(updated ? "状態: 更新あり" : "状態: 最新");
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(_isBusy))
                {
                    if (GUILayout.Button("Import", GUILayout.Width(80), GUILayout.Height(36)))
                    {
                        ImportSingle(file);
                    }
                }

                GUILayout.Space(6);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(4);
                EditorGUILayout.EndVertical();

                GUILayout.Space(3);
            }

            EditorGUILayout.EndScrollView();
        }

        private bool IsUpdated(DriveFileInfo file)
        {
            var record = PackageImportCache.Find(file.id);
            if (record == null || string.IsNullOrEmpty(record.importedModifiedTime)) return true;

            bool cloudParsed = DateTime.TryParse(
                file.modifiedTime, CultureInfo.InvariantCulture, 
                DateTimeStyles.RoundtripKind, out var importedTime
            );

            if (cloudParsed && importedParsed)
            {
                return cloudTime > importedTime;
            }

            // パース不能な場合　文字列比較にフォールバック
            return file.modifiedTime != record.importedModifiedTime;
        }

        private static string FormatDate(string iso)
        {
            if (DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            {
                return dt.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
            }
            return iso;
        }

        private static string FormatSize(string sizeStr)
        {
            if (long.TryParse(sizeStr, out long bytes))
            {
                double mb = bytes / 1024.0 / 1024.0;
                return $"{mb:0.0} MB";
            }
            return "-";
        }

        private void RefreshList()
        {
            _isBusy = true;
            _statusMessage = "更新を確認中...";

            EditorCoroutineUtility.Start(AppsScriptClient.ListUnityPackages(
                _webAppUrl,
                _folderId,
                onSuccess: files =>
                {
                    _driveFiles = files.OrderBy(f => f.name).ToList();
                    _isBusy = false;
                    _statusMessage = $"{_driveFiles.Count}件のパッケージを取得しました";

                    Repaint();
                },
                onError: error =>
                {
                    _isBusy = false;
                    _statusMessage = error;
                    Debug.LogError($"[PackageImporter] {error}");
                    Repaint();
                }
            ));
        }

        private void ImportSingle(DriveInfo file)
        {
            _isBusy = true;
            _statusMessage = $"{file.name} をダウンロードしています...";

            string tempPath = Path.Combine(Path.GetTempPath(), $"{file.id}_{SanitizeFileName(file.name)}");

            EditorCoroutineUtility.Start(AppsScriptClient.DownloadFile(
                _webAppUrl,
                file,
                tempPath,
                onSuccess: Path =>
                {
                    _statusMessage = $"{file.name} をImportしています...";
                    _pendingImports[Path.GetFileNameWithoutExtension(path)] = file;
                    AssetDatabase.ImportPackage(Path, _interactiveImport);
                    _isBusy = false;
                    Repaint();
                },
                onError: error =>
                {
                    _isBusy = false;
                    _statusMessage = error;
                    Debug.LogError($"[PackageImporter] {error}");
                    Repaint();
                }
            ));
        }

        private void ImportAllUpdated()
        {
            var targets = _driveFiles.Where(IsUpdated).ToList();
            if (targets.Count == 0) return;

            _bulkQueue = new Queue<DriveFileInfo>(targets);
            _isBusy = true;
            ProcessBulkQueue();
        }

        private void ProcessBulkQueue()
        {
            if (_bulkQueue.Count == 0)
            {
                _isBusy = false;
                _statusMessage = "一括Importが完了しました";
                Repaint();
                return;
            }

            var file = _bulkQueue.Dequeue();
            _statusMessage = $"{file.name} をダウンロードしています...(残り{_bulkQueue.Count + 1}件) ";
            Repaint();

            string tempPath = Path.Combine(Path.GetTempPath(), $"{file.id}_{SanitizeFileName(file.name)}");

            EditorCoroutineUtility.Start(AppsScriptClient.DownloadFile(
                _webAppUrl,
                file,
                tempPath,
                onSuccess: Path =>
                {
                    _pendingImports[Path.GetFileNameWithoutExtension(path)] = file;
                    _pendingBulkContinue = true;
                    AssetDatabase.ImportPackage(Path, _interactiveImport);
                },
                onError: error =>
                {
                    Debug.LogError($"[PackageImporter] {error}");
                    _statusMessage = error;
                    // 失敗しても次のパッケージ処理へ進む
                    ProcessBulkQueue();
                }
            ));
        }

        private void OnImportPackageCompleted(string packageName)
        {
            if (_pendingImports.TryGetValue(packageName, out var file))
            {
                PackageImportCache.Update(file.id, file.name, file.modifiedTime);
                _pendingImports.Remove(packageName);
                _statusMessage = $"{file.name} のImportが完了しました";
            }

            if (_pendingBulkContinue)
            {
                _pendingBulkContinue = false;
                ProcessBulkQueue();
            }

            Repaint();
        }

        private void OnImportPackageFailed(string packageName, string errorMessage)
        {
            _pendingImports.Remove(packageName);
            _statusMessage = $"Importに失敗しました: {errorMessage}";
            Debug.LogError($"[PackageImporter] Import失敗 ({packageName}): {errorMessage}");

            if (_pendingBulkContinue)
            {
                _pendingBulkContinue = false;
                ProcessBulkQueue();
            }

            Repaint();
        }

        private void OnImportPackageCancelled(string packageName)
        {
            _pendingImports.Remove(packageName);
            _statusMessage = $"Importがキャンセルされました: {packageName}";

            if (_pendingBulkContinue)
            {
                _pendingBulkContinue = false;
                ProcessBulkQueue();
            }

            Repaint();
        }

        private static string SanitizedFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}