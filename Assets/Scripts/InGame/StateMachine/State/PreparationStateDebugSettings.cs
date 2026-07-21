#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace September.Common
{
    public partial class PreparationState
    {
        private static bool _skipPreparationPerformances;

#if UNITY_EDITOR
        private static readonly string DebugKey = "PreparationState.Skip";

        [RuntimeInitializeOnLoadMethod]
        private static void RegisterDebugSetting()
        {
            _skipPreparationPerformances = EditorPrefs.GetBool(DebugKey);

            InGameDebugSetting.OnStart += (setting) =>
            {
                setting.RegisterToggleSetting("ゲーム開始演出をスキップ", value =>
                {
                    EditorPrefs.SetBool(DebugKey, value);
                    _skipPreparationPerformances = value;
                }, defaultValue: _skipPreparationPerformances);
            };
        }
#endif
    }
}
