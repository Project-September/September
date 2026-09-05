#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace September.Editor.HumanoidRig
{
    /// <summary>
    /// Humanoid リグ修正ツールの EditorWindow。
    /// 対象フォルダの管理、問題モデルの一覧表示、選択モデルへの一括修正 (Humanoid 化 / ボーン割当 / T-Pose / Avatar コピー) を行う。
    /// 実処理は各 static クラスに委譲し、このクラスは UI と選択状態のみを扱う。
    /// </summary>
    internal sealed class HumanoidRigFixerWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Rig/Humanoid Rig Fixer";
        private const float PathColumnWidth = 320f;
        private const float StatusColumnWidth = 56f;

        private HumanoidRigTargetFolders _folders;
        private readonly List<HumanoidRigReport> _reports = new List<HumanoidRigReport>();
        private readonly HashSet<string> _selected = new HashSet<string>();
        private Avatar _sourceAvatar;
        private bool _showOnlyProblems = true;
        private Vector2 _scroll;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            var window = GetWindow<HumanoidRigFixerWindow>("Humanoid Rig Fixer");
            window.minSize = new Vector2(720f, 420f);
        }

        private void OnEnable()
        {
            _folders = HumanoidRigTargetFolders.Load();
        }

        private void OnGUI()
        {
            DrawFolderSection();
            EditorGUILayout.Space();
            DrawScanSection();
            EditorGUILayout.Space();
            DrawReportTable();
            EditorGUILayout.Space();
            DrawActionSection();
        }

        // ---------------------------------------------------------------- folders

        private void DrawFolderSection()
        {
            EditorGUILayout.LabelField("対象フォルダ", EditorStyles.boldLabel);

            int removeIndex = -1;
            for (int i = 0; i < _folders.Folders.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool exists = AssetDatabase.IsValidFolder(_folders.Folders[i]);
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.TextField(_folders.Folders[i]);
                    }
                    GUILayout.Label(exists ? "" : "(存在しない)", GUILayout.Width(80f));
                    if (GUILayout.Button("×", GUILayout.Width(24f))) removeIndex = i;
                }
            }
            if (removeIndex >= 0)
            {
                _folders.Folders.RemoveAt(removeIndex);
                _folders.Save();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("フォルダを追加...", GUILayout.Width(140f)))
                {
                    AddFolderFromPanel();
                }
                if (GUILayout.Button("Project 選択中のフォルダを追加", GUILayout.Width(220f)))
                {
                    AddFoldersFromSelection();
                }
            }
        }

        private void AddFolderFromPanel()
        {
            string absolute = EditorUtility.OpenFolderPanel("対象フォルダを選択", Application.dataPath, string.Empty);
            string relative = HumanoidRigTargetFolders.ToProjectRelative(absolute);
            if (string.IsNullOrEmpty(absolute)) return;
            if (relative == null)
            {
                EditorUtility.DisplayDialog("Humanoid Rig Fixer", "Assets フォルダ配下のフォルダを選択してください。", "OK");
                return;
            }
            AddFolder(relative);
        }

        private void AddFoldersFromSelection()
        {
            var folders = Selection.assetGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(AssetDatabase.IsValidFolder)
                .ToList();
            if (folders.Count == 0)
            {
                EditorUtility.DisplayDialog("Humanoid Rig Fixer", "Project ビューでフォルダを選択してから実行してください。", "OK");
                return;
            }
            foreach (var f in folders) AddFolder(f);
        }

        private void AddFolder(string projectRelative)
        {
            if (_folders.Folders.Contains(projectRelative)) return;
            _folders.Folders.Add(projectRelative);
            _folders.Save();
        }

        // ---------------------------------------------------------------- scan

        private void DrawScanSection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("スキャン", GUILayout.Width(120f), GUILayout.Height(24f)))
                {
                    Scan();
                }
                _showOnlyProblems = GUILayout.Toggle(_showOnlyProblems, "問題のあるモデルのみ表示");
                GUILayout.FlexibleSpace();
                int problems = _reports.Count(r => r.HasProblem);
                GUILayout.Label($"モデル {_reports.Count} 件 / 問題 {problems} 件");
            }
        }

        private void Scan()
        {
            _reports.Clear();
            _selected.Clear();

            var paths = _folders.FindModelPaths();
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Humanoid Rig Fixer: スキャン中", paths[i], (float)i / Math.Max(1, paths.Count));
                    _reports.Add(HumanoidRigDiagnoser.Diagnose(paths[i]));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            foreach (var missing in _folders.InvalidFolders)
            {
                Debug.LogWarning($"[HumanoidRigFixer] フォルダが存在しません: {missing}");
            }
            Debug.Log($"[HumanoidRigFixer] スキャン完了: {_reports.Count} 件中 問題 {_reports.Count(r => r.HasProblem)} 件");
        }

        // ---------------------------------------------------------------- table

        private IEnumerable<HumanoidRigReport> VisibleReports =>
            _showOnlyProblems ? _reports.Where(r => r.HasProblem) : _reports;

        private void DrawReportTable()
        {
            var visible = VisibleReports.ToList();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("全選択", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    foreach (var r in visible) _selected.Add(r.AssetPath);
                }
                if (GUILayout.Button("全解除", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    _selected.Clear();
                }
                GUILayout.Label("パス", GUILayout.Width(PathColumnWidth));
                GUILayout.Label("状態", GUILayout.Width(StatusColumnWidth));
                GUILayout.Label("問題");
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;
                if (visible.Count == 0)
                {
                    EditorGUILayout.HelpBox(_reports.Count == 0 ? "スキャンを実行してください。" : "表示対象のモデルはありません。", MessageType.Info);
                }
                foreach (var report in visible) DrawReportRow(report);
            }
        }

        private void DrawReportRow(HumanoidRigReport report)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                bool selected = _selected.Contains(report.AssetPath);
                bool now = EditorGUILayout.Toggle(selected, GUILayout.Width(18f));
                if (now != selected)
                {
                    if (now) _selected.Add(report.AssetPath);
                    else _selected.Remove(report.AssetPath);
                }

                if (GUILayout.Button(report.AssetPath, EditorStyles.linkLabel, GUILayout.Width(PathColumnWidth)))
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(report.AssetPath));
                }

                var style = new GUIStyle(EditorStyles.label);
                style.normal.textColor = report.HasError ? Color.red : report.HasProblem ? new Color(0.9f, 0.6f, 0f) : Color.green;
                GUILayout.Label(report.StatusLabel, style, GUILayout.Width(StatusColumnWidth));

                EditorGUILayout.LabelField(report.HasProblem ? report.IssueSummary : $"{report.AvatarSetup}", EditorStyles.wordWrappedLabel);
            }
        }

        // ---------------------------------------------------------------- actions

        private void DrawActionSection()
        {
            EditorGUILayout.LabelField($"選択中 {_selected.Count} 件への操作", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(_selected.Count == 0))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Humanoid 化"))
                    {
                        RunBatch("Humanoid 化", p => HumanoidAnimationTypeSetter.EnsureHumanoid(p) ? "変更" : "既に Humanoid");
                    }
                    if (GUILayout.Button("命名規則でボーン再割当"))
                    {
                        RunBatch("命名規則でボーン再割当", p => HumanoidBoneMappingApplier.ApplyByName(p).Summarize());
                    }
                    if (GUILayout.Button("Unity 自動割当に戻す"))
                    {
                        RunBatch("Unity 自動割当に戻す", p => { HumanoidBoneMappingApplier.ResetToAutoMapping(p); return null; });
                    }
                    if (GUILayout.Button("T-Pose 強制"))
                    {
                        RunBatch("T-Pose 強制", p => HumanoidTPoseEnforcer.Enforce(p) == TPoseResult.Fixed ? "補正" : "既に有効");
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _sourceAvatar = (Avatar)EditorGUILayout.ObjectField("コピー元 Avatar", _sourceAvatar, typeof(Avatar), false);
                    using (new EditorGUI.DisabledScope(_sourceAvatar == null))
                    {
                        if (GUILayout.Button("Avatar をコピーして統一", GUILayout.Width(180f)))
                        {
                            var source = _sourceAvatar;
                            RunBatch("Avatar コピー", p => { AvatarSourceCopier.Apply(p, source); return null; });
                        }
                    }
                }
            }
        }

        private void RunBatch(string title, Func<string, string> action)
        {
            var targets = _selected.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
            bool ok = EditorUtility.DisplayDialog(
                "Humanoid Rig Fixer",
                $"{title} を {targets.Count} 件のモデルに適用し再インポートします。\n(.meta のインポート設定が書き換わります)",
                "実行", "キャンセル");
            if (!ok) return;

            var outcome = HumanoidRigBatchRunner.Run(title, targets, action);
            Scan();

            if (outcome.Failures.Count > 0)
            {
                EditorUtility.DisplayDialog("Humanoid Rig Fixer", HumanoidRigBatchRunner.Summarize(title, outcome), "OK");
            }
        }
    }
}
#endif
