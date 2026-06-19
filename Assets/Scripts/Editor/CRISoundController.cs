#if UNITY_EDITOR
using CRISound;
using UnityEditor;
using UnityEngine;

namespace September.Editor
{
    public class CRISoundController : EditorWindow
    {
        private static bool _soundEnabled = true;
        private static bool _initialized = false;

        [MenuItem("Tools/CRI Sound Controller")]
        public static void ShowWindow()
        {
            GetWindow<CRISoundController>("CRI Sound Controller");
        }

        private void OnEnable()
        {
            if (!_initialized)
            {
                _soundEnabled = EditorPrefs.GetBool("CRISoundController_Enabled", true);
                _initialized = true;
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("CRI Sound Controller", EditorStyles.boldLabel);
            GUILayout.Space(10);

            bool newSoundEnabled = EditorGUILayout.Toggle("Sound Enabled", _soundEnabled);
            
            if (newSoundEnabled != _soundEnabled)
            {
                _soundEnabled = newSoundEnabled;
                EditorPrefs.SetBool("CRISoundController_Enabled", _soundEnabled);
                ApplySoundSetting();
            }

            GUILayout.Space(10);
            
            if (GUILayout.Button("Apply Setting"))
            {
                ApplySoundSetting();
            }

            GUILayout.Space(10);
            GUILayout.Label($"Current Status: {(_soundEnabled ? "Enabled" : "Disabled")}", EditorStyles.helpBox);
        }

        private void ApplySoundSetting()
        {
            if (Application.isPlaying && CuePlayAtomExPlayer.Instance.IsReady)
            {
                float volume = _soundEnabled ? 1.0f : 0.0f;
                
                CuePlayAtomExPlayer.Instance.Player(SoundType.BGM).SetVolume(volume);
                CuePlayAtomExPlayer.Instance.Player(SoundType.SE).SetVolume(volume);
                CuePlayAtomExPlayer.Instance.Player(SoundType.Voice).SetVolume(volume);
                
                Debug.Log($"CRI Sound {(_soundEnabled ? "Enabled" : "Disabled")}");
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _soundEnabled = EditorPrefs.GetBool("CRISoundController_Enabled", true);
                
                // プレイモード開始時に設定を適用（少し遅延させる）
                EditorApplication.delayCall += () =>
                {
                    if (Application.isPlaying && CuePlayAtomExPlayer.Instance.IsReady)
                    {
                        float volume = _soundEnabled ? 1.0f : 0.0f;
                        
                        CuePlayAtomExPlayer.Instance.Player(SoundType.BGM).SetVolume(volume);
                        CuePlayAtomExPlayer.Instance.Player(SoundType.SE).SetVolume(volume);
                        CuePlayAtomExPlayer.Instance.Player(SoundType.Voice).SetVolume(volume);
                    }
                };
            }
        }
    }
}
#endif