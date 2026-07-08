using System.Linq;
using UnityEditor;
using UnityEngine;

namespace September.Editor.PackageImporter
{
    public class PackageImporter : EditorWindow
    {
        private PackageImportController _controller;
        private PackageProgressInfo _currentProgress = new() { Progress = 0f, Status = "", Detail = "" };
        private string _statusMessage = PackageImportConstants.Messages.ImportWaiting;
        private Vector2 _packageScrollPosition;
        private Vector2 _googleDriveScrollPosition;
        private float _lastRepaintTime;

        [MenuItem(PackageImportConstants.MenuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<PackageImporter>(PackageImportConstants.WindowTitle);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeController();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            _controller?.Dispose();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space();

            DrawToolbar();
            EditorGUILayout.Space();

            DrawGoogleDriveSection();
            EditorGUILayout.Space();

            DrawReleaseSelection();
            EditorGUILayout.Space();

            DrawPackageList();
            EditorGUILayout.Space();

            DrawProgressSection();
            EditorGUILayout.Space();

            DrawSettings();
        }

        private void InitializeController()
        {
            _controller = new PackageImportController();
            _controller.OnProgressChanged += OnProgressChanged;
            _controller.OnStatusChanged += OnStatusChanged;
            _ = _controller.InitializeAsync();
        }

        private void DrawHeader()
        {
            DrawColoredLabel(PackageImportConstants.Messages.DoNotPlayScene, Color.red);
            EditorGUILayout.LabelField(_statusMessage, EditorStyles.miniLabel);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                if (GUILayout.Button("パッケージ一覧の同期", GUILayout.Width(150)))
                {
                    _ = _controller.InitializeAsync();
                }

                GUILayout.FlexibleSpace();

                GUI.enabled = CanImportReleasePackage() && _controller.SelectedPackages.Count > 0;
                if (GUILayout.Button("選択中リリースをすべてImport", GUILayout.Width(190)))
                {
                    _ = _controller.ImportSelectedReleasePackagesAsync();
                }
                GUI.enabled = true;
            }
        }

        private void DrawGoogleDriveSection()
        {
            EditorGUILayout.LabelField("Google Drive直接取得", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PackageImportSettings.GoogleDriveFolderId = EditorGUILayout.TextField(
                    "Folder ID",
                    PackageImportSettings.GoogleDriveFolderId);

                PackageImportSettings.GoogleDriveApiKey = EditorGUILayout.PasswordField(
                    "API Key",
                    PackageImportSettings.GoogleDriveApiKey);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = CanRunImport() && HasGoogleDriveSettings();
                    if (GUILayout.Button("差分を取得", GUILayout.Width(120)))
                    {
                        _ = _controller.SyncGoogleDrivePackagesAsync();
                    }

                    GUI.enabled = CanRunImport() && _controller.ChangedGoogleDrivePackages.Count > 0;
                    if (GUILayout.Button($"差分のみImport ({_controller.ChangedGoogleDrivePackages.Count})", GUILayout.Width(160)))
                    {
                        _ = _controller.ImportChangedGoogleDrivePackagesAsync();
                    }

                    GUI.enabled = true;
                    GUILayout.FlexibleSpace();
                }

                if (!HasGoogleDriveSettings())
                {
                    EditorGUILayout.HelpBox(PackageImportConstants.Messages.GoogleDriveSettingsRequired, MessageType.Info);
                    return;
                }

                DrawGoogleDrivePackageList();
            }
        }

        private void DrawGoogleDrivePackageList()
        {
            var packages = _controller?.GoogleDrivePackages;
            if (packages == null || packages.Count == 0)
            {
                EditorGUILayout.HelpBox(PackageImportConstants.Messages.NoGoogleDrivePackages, MessageType.Info);
                return;
            }

            _googleDriveScrollPosition = EditorGUILayout.BeginScrollView(_googleDriveScrollPosition, GUILayout.Height(180));
            foreach (var packageAsset in packages)
            {
                DrawGoogleDrivePackageRow(packageAsset);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawGoogleDrivePackageRow(PackageAsset packageAsset)
        {
            var hasChanged = GoogleDrivePackageImportHistory.HasChanged(packageAsset);
            var hasHistory = GoogleDrivePackageImportHistory.HasImportHistory(packageAsset);
            var status = hasChanged ? (hasHistory ? "[UPDATED]" : "[NEW]") : "[IMPORTED]";

            using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea))
            {
                GUI.contentColor = hasChanged ? Color.yellow : Color.gray;
                EditorGUILayout.LabelField(status, GUILayout.Width(80));
                GUI.contentColor = Color.white;

                EditorGUILayout.LabelField(packageAsset.Name, GUILayout.MinWidth(180));
                EditorGUILayout.LabelField(PackageFileUtility.FormatFileSize(packageAsset.Size), GUILayout.Width(80));
                EditorGUILayout.LabelField(FormatModifiedTime(packageAsset.ModifiedTime), GUILayout.Width(135));

                GUI.enabled = CanRunImport();
                if (GUILayout.Button("Import", GUILayout.Width(70)))
                {
                    _ = _controller.ImportGoogleDrivePackageAsync(packageAsset);
                }

                GUI.enabled = true;
                GUI.contentColor = Color.white;
            }
        }

        private void DrawReleaseSelection()
        {
            if (_controller?.ReleaseNames?.Count == 0) return;

            var releaseNames = _controller.ReleaseNames.ToArray();
            _controller.SelectedReleaseIndex = EditorGUILayout.Popup(
                "パッケージバージョン",
                _controller.SelectedReleaseIndex,
                releaseNames);
        }

        private void DrawPackageList()
        {
            EditorGUILayout.LabelField("インポート可能なパッケージ", EditorStyles.boldLabel);

            var packages = _controller?.SelectedRelease.Assets?.OrderBy(a => a.Name).ToList();
            if (packages == null || packages.Count == 0)
            {
                EditorGUILayout.HelpBox(PackageImportConstants.Messages.NoPackages, MessageType.Info);
                return;
            }

            _packageScrollPosition = EditorGUILayout.BeginScrollView(_packageScrollPosition, GUILayout.ExpandHeight(true));
            foreach (var packageAsset in packages)
            {
                DrawPackageRow(packageAsset);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawPackageRow(PackageAsset packageAsset)
        {
            var isUnityPackage = PackageFileUtility.IsUnityPackage(packageAsset);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea))
            {
                GUI.contentColor = isUnityPackage ? Color.white : Color.gray;
                EditorGUILayout.LabelField(packageAsset.Name, GUILayout.MinWidth(180));
                EditorGUILayout.LabelField(PackageFileUtility.FormatFileSize(packageAsset.Size), GUILayout.Width(80));

                GUI.enabled = CanImportReleasePackage() && isUnityPackage;
                if (GUILayout.Button("Import", GUILayout.Width(70)))
                {
                    _ = _controller.ImportPackageAsync(packageAsset);
                }

                GUI.enabled = true;
                GUI.contentColor = Color.white;
            }
        }

        private void DrawProgressSection()
        {
            if (_controller?.IsImporting != true) return;

            if (!string.IsNullOrEmpty(_currentProgress.Status))
            {
                DrawColoredLabel(_currentProgress.Status, Color.cyan);
            }

            var rect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            var progressValue = Mathf.Clamp01(_currentProgress.Progress);
            var progressText = !string.IsNullOrEmpty(_currentProgress.Detail)
                ? _currentProgress.Detail
                : $"進捗: {progressValue * 100:F1}%";

            EditorGUI.ProgressBar(rect, progressValue, progressText);
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("設定", EditorStyles.boldLabel);

            PackageImportSettings.ShowImportWindow = GUILayout.Toggle(
                PackageImportSettings.ShowImportWindow,
                "UnityPackageの手動インポート画面を表示");

            PackageImportSettings.KeepDownloadedPackage = GUILayout.Toggle(
                PackageImportSettings.KeepDownloadedPackage,
                "ダウンロードしたUnityPackageを削除しない");

            PackageImportSettings.UseLogger = GUILayout.Toggle(
                PackageImportSettings.UseLogger,
                "Log出力を有効化");
        }

        private bool CanImportReleasePackage()
        {
            return CanRunImport() && (_controller.ReleaseNames?.Count ?? 0) > 0;
        }

        private bool CanRunImport()
        {
            return _controller != null &&
                   !_controller.IsImporting;
        }

        private bool HasGoogleDriveSettings()
        {
            return !string.IsNullOrWhiteSpace(PackageImportSettings.GoogleDriveFolderId) &&
                   !string.IsNullOrWhiteSpace(PackageImportSettings.GoogleDriveApiKey);
        }

        private string FormatModifiedTime(string modifiedTime)
        {
            if (System.DateTime.TryParse(modifiedTime, out var dateTime))
            {
                return dateTime.ToLocalTime().ToString("yy/MM/dd HH:mm");
            }

            return "-";
        }

        private void DrawColoredLabel(string text, Color color)
        {
            var style = GUI.skin.label;
            var originalColor = style.normal.textColor;
            style.normal.textColor = color;
            GUILayout.Label(text, style);
            style.normal.textColor = originalColor;
        }

        private void OnProgressChanged(PackageProgressInfo progressInfo)
        {
            _currentProgress = progressInfo;
            Repaint();
        }

        private void OnStatusChanged(string status)
        {
            _statusMessage = status;
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (_controller?.IsImporting != true) return;

            if (Time.realtimeSinceStartup - _lastRepaintTime > 0.1f)
            {
                Repaint();
                _lastRepaintTime = Time.realtimeSinceStartup;
            }
        }
    }
}
