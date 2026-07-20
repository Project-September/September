using System;
using System.Collections.Generic;
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

            foreach (var)
        }
    }
}