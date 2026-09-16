using CriWare;
using UnityEditor;
using UnityEngine;

namespace CRISound.Editor
{
    public sealed class SoundVolumeSettingsWindow : EditorWindow
    {
        private const string AssetPath = "Assets/Resources/SoundVolumeSettings.asset";
        private const string CueSheetName = "ALLCue";
        private const string AcfFileName = "September.acf";

        private SoundVolumeSettings _settings;
        private SerializedObject _serializedSettings;
        private Vector2 _scrollPosition;
        private string _searchText = string.Empty;
        private CriWare.Editor.CriAtomEditorUtilities.PreviewPlayer _previewPlayer;
        private CriAtomExAcb _previewAcb;
        private bool _isPreviewAcfRegistered;

        [MenuItem("September/Sound/Volume Settings")]
        private static void Open()
        {
            GetWindow<SoundVolumeSettingsWindow>("Sound Volume Settings");
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void OnDisable()
        {
            StopPreview();
            DisposePreview();
        }

        private void OnGUI()
        {
            if (_serializedSettings == null || _serializedSettings.targetObject == null)
            {
                LoadSettings();
            }

            if (_serializedSettings == null)
            {
                EditorGUILayout.HelpBox("SoundVolumeSettings.assetを作成できませんでした。", MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                "各値はCRI側で設定された音量への倍率です。September.acfを使用してゲームと同じ音響設定で試聴します。",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(Application.isPlaying && !CuePlayAtomExPlayer.Instance.IsReady))
            {
                if (GUILayout.Button("試聴を停止"))
                {
                    StopPreview();
                }
            }

            _searchText = EditorGUILayout.TextField("検索", _searchText);

            _serializedSettings.Update();
            SerializedProperty entries = _serializedSettings.FindProperty("_entries");
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                SerializedProperty cueName = entry.FindPropertyRelative("CueName");
                SerializedProperty volume = entry.FindPropertyRelative("Volume");

                if (!string.IsNullOrEmpty(_searchText) &&
                    cueName.stringValue.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(cueName.stringValue, GUILayout.MinWidth(240f));
                EditorGUILayout.PropertyField(volume, GUIContent.none, GUILayout.Width(80f));

                using (new EditorGUI.DisabledScope(Application.isPlaying && !CuePlayAtomExPlayer.Instance.IsReady))
                {
                    if (GUILayout.Button("再生", GUILayout.Width(48f)))
                    {
                        _serializedSettings.ApplyModifiedProperties();
                        PlayPreview(cueName.stringValue, volume.floatValue);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            _serializedSettings.ApplyModifiedProperties();
        }

        private void LoadSettings()
        {
            _settings = AssetDatabase.LoadAssetAtPath<SoundVolumeSettings>(AssetPath);
            if (_settings == null)
            {
                _settings = CreateInstance<SoundVolumeSettings>();
                AssetDatabase.CreateAsset(_settings, AssetPath);
                AssetDatabase.SaveAssets();
            }

            _serializedSettings = new SerializedObject(_settings);
        }

        private void PlayPreview(string cueName, float volume)
        {
            StopPreview();

            if (!Application.isPlaying)
            {
                if (!EnsureEditorPreview()) return;

                _previewPlayer.player.SetVolume(Mathf.Max(0f, volume));
                _previewPlayer.Play(_previewAcb, cueName);
                return;
            }

            SoundType soundType = cueName.StartsWith("BGM_", System.StringComparison.Ordinal)
                ? SoundType.BGM
                : cueName.StartsWith("VO_", System.StringComparison.Ordinal)
                    ? SoundType.Voice
                    : SoundType.SE;

            CuePlayAtomExPlayer.Instance.Player(soundType)
                .Play(CueSheetName, cueName, 0f, volume);
        }

        private void StopPreview()
        {
            if (!Application.isPlaying)
            {
                _previewPlayer?.Stop(true);
                return;
            }

            if (!CuePlayAtomExPlayer.Instance.IsReady) return;

            CuePlayAtomExPlayer.Instance.Player(SoundType.BGM).Stop();
            CuePlayAtomExPlayer.Instance.Player(SoundType.SE).Stop();
            CuePlayAtomExPlayer.Instance.Player(SoundType.Voice).Stop();
        }

        private bool EnsureEditorPreview()
        {
            if (_previewPlayer == null)
            {
                _previewPlayer = new CriWare.Editor.CriAtomEditorUtilities.PreviewPlayer();
            }

            if (!_isPreviewAcfRegistered)
            {
                string acfPath = System.IO.Path.Combine(Application.streamingAssetsPath, AcfFileName);
                CriAtomEx.UnregisterAcf();
                _isPreviewAcfRegistered = CriAtomEx.RegisterAcf(null, acfPath);

                if (!_isPreviewAcfRegistered)
                {
                    Debug.LogWarning($"[SoundVolumeSettings] {AcfFileName}を試聴用に読み込めませんでした。");
                    return false;
                }
            }

            if (_previewAcb != null) return true;

            string acbPath = System.IO.Path.Combine(Application.streamingAssetsPath, "ALLCue.acb");
            string awbPath = System.IO.Path.Combine(Application.streamingAssetsPath, "ALLCue.awb");
            _previewAcb = CriWare.Editor.CriAtomEditorUtilities.LoadAcbFile(null, acbPath, awbPath);

            if (_previewAcb != null) return true;

            Debug.LogWarning("[SoundVolumeSettings] ALLCue.acbを試聴用に読み込めませんでした。");
            return false;
        }

        private void DisposePreview()
        {
            _previewAcb?.Dispose();
            _previewAcb = null;

            _previewPlayer?.Dispose();
            _previewPlayer = null;

            if (_isPreviewAcfRegistered && !Application.isPlaying)
            {
                CriAtomEx.UnregisterAcf();
            }

            _isPreviewAcfRegistered = false;
        }
    }
}
