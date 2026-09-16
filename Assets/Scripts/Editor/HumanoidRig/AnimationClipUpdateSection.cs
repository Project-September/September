#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace September.Editor.HumanoidRig
{
    internal sealed class AnimationClipUpdateSection
    {
        private enum ResultFilter { All, Ready, Missing, Ambiguous }
        private static readonly string[] FilterLabels = { "すべて", "更新可能", "同名 FBX クリップなし", "更新元重複" };
        private readonly Dictionary<string, ResultFilter> _results = new Dictionary<string, ResultFilter>();
        private ResultFilter _filter;
        private readonly HumanoidRigTargetFolders _folders;
        private readonly ModelPathSelectionList _list = new ModelPathSelectionList();
        private readonly Dictionary<string, AnimationClip> _sources = new Dictionary<string, AnimationClip>();
        private readonly Dictionary<string, string> _details = new Dictionary<string, string>();
        private DefaultAsset _scanFolder;
        private string _summary;

        public AnimationClipUpdateSection(HumanoidRigTargetFolders folders) => _folders = folders;

        public void Draw()
        {
            EditorGUILayout.HelpBox(
                "対象フォルダ内の FBX と .anim を検索し、同名クリップを更新します。\n" +
                "イベント（時刻・引数を含む）とアセット参照を保持します。それ以外のクリップ設定は FBX からコピーします。\n" +
                "未指定の場合は上の対象フォルダを使用します。内包クリップが1件の FBX はファイル名でも照合します。\n" +
                "更新元が同名で複数ある場合は除外します。",
                MessageType.Info);
            _scanFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "対象フォルダ（FBX / .anim）", _scanFolder, typeof(DefaultAsset), false);
            string destination = AssetDatabase.GetAssetPath(_scanFolder);
            bool valid = _scanFolder == null || AssetDatabase.IsValidFolder(destination);
            if (!valid) EditorGUILayout.HelpBox("対象にはフォルダを指定してください。", MessageType.Warning);
            using (new EditorGUI.DisabledScope(!valid))
                if (GUILayout.Button("FBX と同名クリップをスキャン")) Scan();

            if (!string.IsNullOrEmpty(_summary)) EditorGUILayout.HelpBox(_summary, MessageType.Info);
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                _filter = (ResultFilter)EditorGUILayout.Popup("表示フィルター", (int)_filter, FilterLabels);
                if (change.changed) ApplyFilter();
            }
            EditorGUILayout.LabelField($"表示 {_list.Count} 件（更新可能な行のみ選択できます）");
            _list.Draw("更新元 FBX / 詳細", RowInfo,
                "該当するクリップがありません。未スキャンの場合は対象フォルダを指定してスキャンしてください。",
                path => _sources.ContainsKey(path));
            using (new EditorGUI.DisabledScope(_list.SelectedCount == 0 || !valid))
                if (GUILayout.Button($"選択中 {_list.SelectedCount} 件のクリップを更新（イベント保持）")) Run();
        }

        private Dictionary<string, List<AnimationClip>> FindSources()
        {
            var folders = ScanFolders();
            var result = new Dictionary<string, List<AnimationClip>>(StringComparer.Ordinal);
            if (folders.Length == 0) return result;
            var paths = AssetDatabase.FindAssets("t:Model", folders)
                .Select(AssetDatabase.GUIDToAssetPath).Distinct()
                .Where(path => string.Equals(Path.GetExtension(path), ".fbx", StringComparison.OrdinalIgnoreCase))
                .Where(path => AssetImporter.GetAtPath(path) is ModelImporter);
            foreach (string path in paths)
            {
                var clips = AnimationClipBindings.LoadClips(path);
                foreach (var clip in clips) AddSource(result, clip.name, clip);
                // Take 001 等の内包名でも、単一クリップなら同名 FBX と安全に対応付けられる。
                if (clips.Count == 1) AddSource(result, Path.GetFileNameWithoutExtension(path), clips[0]);
            }
            return result;
        }

        private static void AddSource(Dictionary<string, List<AnimationClip>> sources, string name, AnimationClip clip)
        {
            if (!sources.TryGetValue(name, out var matches))
                sources.Add(name, matches = new List<AnimationClip>());
            if (!matches.Contains(clip)) matches.Add(clip);
        }

        private static List<AnimationClip> FindMatches(Dictionary<string, List<AnimationClip>> sources,
            string path, AnimationClip target)
        {
            // ファイル名と内部名の両方を確認し、異なる更新元が見つかった場合は重複として扱う。
            var matches = new List<AnimationClip>();
            if (sources.TryGetValue(target.name, out var byName)) matches.AddRange(byName);
            if (sources.TryGetValue(Path.GetFileNameWithoutExtension(path), out var byFile)) matches.AddRange(byFile);
            return matches.Distinct().ToList();
        }

        private string[] ScanFolders() => _scanFolder == null
            ? _folders.ValidFolders.ToArray()
            : new[] { AssetDatabase.GetAssetPath(_scanFolder) };

        private void Scan()
        {
            _sources.Clear();
            _details.Clear();
            _results.Clear();
            var sources = FindSources();
            var folders = ScanFolders();
            int missing = 0, ambiguous = 0;
            if (folders.Length > 0)
            {
                foreach (string path in AssetDatabase.FindAssets("t:AnimationClip", folders)
                    .Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(path => path, StringComparer.Ordinal))
                {
                    if (!string.Equals(Path.GetExtension(path), ".anim", StringComparison.OrdinalIgnoreCase)) continue;
                    var target = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (target == null) continue;
                    var matches = FindMatches(sources, path, target);
                    if (matches.Count == 0)
                    {
                        missing++;
                        _results.Add(path, ResultFilter.Missing);
                        _details.Add(path, $"同名 FBX クリップなし\n照合名: {target.name} / ファイル名: {Path.GetFileNameWithoutExtension(path)}");
                        continue;
                    }
                    if (matches.Count != 1)
                    {
                        ambiguous++;
                        _results.Add(path, ResultFilter.Ambiguous);
                        _details.Add(path, "更新元候補が複数あります:\n" + string.Join("\n",
                            matches.Select(clip => $"{AssetDatabase.GetAssetPath(clip)} / {clip.name}")));
                        continue;
                    }
                    _results.Add(path, ResultFilter.Ready);
                    _sources.Add(path, matches[0]);
                    _details.Add(path, $"{AssetDatabase.GetAssetPath(matches[0])} / {matches[0].name}\n" +
                        $"保持するイベント: {AnimationUtility.GetAnimationEvents(target).Length} 件");
                }
            }
            ApplyFilter();
            _summary = $"更新可能 {_sources.Count} 件 / 同名 FBX クリップなし {missing} 件 / 更新元重複で除外 {ambiguous} 件";
        }

        private void ApplyFilter()
        {
            // 非表示の行は選択から外し、表示しているクリップだけを更新対象にする。
            _list.SetPaths(_results.Where(pair => _filter == ResultFilter.All || pair.Value == _filter)
                .Select(pair => pair.Key).OrderBy(path => path, StringComparer.Ordinal));
        }

        private ModelRowInfo RowInfo(string path)
        {
            switch (_results[path])
            {
                case ResultFilter.Ready:
                    return new ModelRowInfo("更新可能", Color.green, _details[path]);
                case ResultFilter.Missing:
                    return new ModelRowInfo("該当なし", Color.yellow, _details[path]);
                default:
                    return new ModelRowInfo("重複", Color.yellow, _details[path]);
            }
        }

        private void Run()
        {
            // スキャン後のフォルダ変更・再インポートによる誤更新を防ぐため、実行時にも照合する。
            var currentSources = FindSources();
            var folders = ScanFolders();
            var targets = _list.Selected.Where(path => _sources.ContainsKey(path)).ToList();
            if (targets.Count == 0) return;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("FBX からクリップ更新");
            try
            {
                HumanoidRigBatchPrompt.Run("クリップ更新",
                    $"選択中 {targets.Count} 件の .anim を一覧の FBX で更新します。\n" +
                    "既存イベントの時刻・引数を保持します。クリップの長さが変わってもイベント時刻は変更しません。\nUndo で元に戻せます。",
                    targets, path =>
                    {
                        var target = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                        if (target == null || !folders.Any(folder => path.StartsWith(folder.TrimEnd('/') + "/", StringComparison.Ordinal)))
                            throw new InvalidOperationException("更新先が変更されています。再スキャンしてください。");
                        var matches = FindMatches(currentSources, path, target);
                        if (matches.Count != 1 || matches[0] != _sources[path])
                            throw new InvalidOperationException("更新元が変更されています。再スキャンしてください。");
                        if (!AssetDatabase.IsOpenForEdit(path))
                            throw new InvalidOperationException("更新先が書き込み可能ではありません。");
                        UpdateClip(matches[0], target);
                        return $"イベント {AnimationUtility.GetAnimationEvents(target).Length} 件を保持";
                    });
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
            Scan();
        }

        internal static void UpdateClip(AnimationClip source, AnimationClip target)
        {
            var events = AnimationUtility.GetAnimationEvents(target);
            string name = target.name;
            var flags = target.hideFlags;
            Undo.RegisterCompleteObjectUndo(target, "FBX からクリップ更新");
            // Humanoid の内部カーブも含めてコピーし、既存オブジェクトの GUID / fileID を維持する。
            EditorUtility.CopySerialized(source, target);
            target.name = name;
            target.hideFlags = flags;
            AnimationUtility.SetAnimationEvents(target, events);
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
