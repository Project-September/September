#if UNITY_EDITOR
using InGame.Exhibit.Candle;
using InGame.Exhibit.HazardTrail;
using InGame.Common;
using InGame.Interact;
using September.Lesson;
using UnityEditor;
using UnityEngine;

namespace September.Editor.Lesson
{
    /// <summary>
    /// 既存のキャンドル展示物を元に、授業で使う完成済みPrefabを2種類作るEditor専用クラス。
    /// ゲームの実行中には使われない。
    /// </summary>
    [InitializeOnLoad]
    internal static class CreateLessonExhibitPrefabs
    {
        // 複製元の展示物と、完成したPrefabの保存先。
        private const string SourcePrefabPath = "Assets/Prefabs/Exhibit/Candle/Candle Interact.prefab";
        private const string SpeedPrefabPath = "Assets/Lesson/Prefabs/Lesson Speed Exhibit.prefab";
        private const string VisualPrefabPath = "Assets/Lesson/Prefabs/Lesson Visual Exhibit.prefab";
        // Unityの再読み込みで同じ処理が繰り返されるのを防ぐためのキー。
        private const string SessionKey = "September.Lesson.CreateLessonExhibitPrefabs";

        /// <summary>
        /// Editorの読み込み後に一度だけPrefab作成を予約する。
        /// </summary>
        static CreateLessonExhibitPrefabs()
        {
            // 既存の授業用Prefabを再生成するとInspectorでの変更が消えるため、初回だけ作る。
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SpeedPrefabPath) &&
                AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath)) return;
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += CreatePrefabs;
        }

        /// <summary>
        /// メニューからも実行できる入口。元Prefabを読み込んで2種類のPrefabを作る。
        /// </summary>
        [MenuItem("September/Lesson/Create Sample Exhibit Prefabs")]
        public static void CreatePrefabs()
        {
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (!sourcePrefab)
            {
                Debug.LogError("Lesson展示物の作成に必要な元Prefabが見つかりません。");
                return;
            }

            // 保存先のフォルダがまだなければ作成する。
            if (!AssetDatabase.IsValidFolder("Assets/Lesson/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Lesson", "Prefabs");
            CreateSpeedPrefab();
            CreateVisualPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Lesson展示物Prefabを作成しました。");
        }

        /// <summary>
        /// キャンドル展示物を複製し、インタラクト効果を速度アップに置き換える。
        /// </summary>
        private static void CreateSpeedPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
            try
            {
                root.name = "Lesson Speed Exhibit";
                RemoveCandleEffects(root);
                // SerializedObjectはPrefabのInspectorに保存される値を書き換えるために使う。
                SetInteractEffect(root, new LessonSpeedInteractEffect(), effect =>
                {
                    var serializedEffect = new SerializedObject(root.GetComponent<InteractableBase>());
                    var effectProperty = serializedEffect.FindProperty("_characterEffects").GetArrayElementAtIndex(0);
                    effectProperty.FindPropertyRelative("_duration").floatValue = 10f;
                    effectProperty.FindPropertyRelative("_speedMultiplier").floatValue = 5f;
                    serializedEffect.ApplyModifiedPropertiesWithoutUndo();
                });
                PrefabUtility.SaveAsPrefabAsset(root, SpeedPrefabPath);
            }
            finally
            {
                // 保存に失敗した場合も、Editor内の一時的なPrefabを片付ける。
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// キャンドル展示物を複製し、色や大きさが変化する展示物へ置き換える。
        /// </summary>
        private static void CreateVisualPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
            try
            {
                root.name = "Lesson Visual Exhibit";
                RemoveCandleEffects(root);
                var visual = root.GetComponent<LessonVisualExhibit>();
                if (!visual) visual = root.AddComponent<LessonVisualExhibit>();
                // 見た目を変えるスクリプトに、対象のRendererとTransformを接続する。
                var visualProperties = new SerializedObject(visual);
                visualProperties.FindProperty("_targetRenderer").objectReferenceValue = root.GetComponentInChildren<Renderer>(true);
                visualProperties.FindProperty("_visualTransform").objectReferenceValue = root.transform;
                visualProperties.ApplyModifiedPropertiesWithoutUndo();
                SetInteractEffect(root, new LessonVisualInteractEffect(), effect =>
                {
                    var serializedEffect = new SerializedObject(root.GetComponent<InteractableBase>());
                    var effectProperty = serializedEffect.FindProperty("_characterEffects").GetArrayElementAtIndex(0);
                    effectProperty.FindPropertyRelative("_visualExhibit").objectReferenceValue = visual;
                    serializedEffect.ApplyModifiedPropertiesWithoutUndo();
                });
                PrefabUtility.SaveAsPrefabAsset(root, VisualPrefabPath);
            }
            finally
            {
                // 保存後は読み込んだPrefabの一時オブジェクトを解放する。
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 元のキャンドル固有の攻撃と足跡の部品を外し、授業用の効果だけを残す。
        /// </summary>
        private static void RemoveCandleEffects(GameObject root)
        {
            var candleInvoker = root.GetComponent<CandleInteractInvoker>();
            if (candleInvoker) Object.DestroyImmediate(candleInvoker);

            var trailEmitter = root.GetComponent<HazardTrailEmitter>();
            if (trailEmitter) Object.DestroyImmediate(trailEmitter);
        }

        /// <summary>
        /// 展示物の効果リストを一件にし、Prefabに保存するための設定を書き込む。
        /// </summary>
        private static void SetInteractEffect(GameObject root, CharacterInteractEffectBase effect, System.Action<CharacterInteractEffectBase> configure)
        {
            var interactable = root.GetComponent<InteractableBase>();
            var serializedInteractable = new SerializedObject(interactable);
            var effects = serializedInteractable.FindProperty("_characterEffects");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).managedReferenceValue = effect;
            serializedInteractable.ApplyModifiedPropertiesWithoutUndo();
            configure(effect);
        }
    }
}
#endif
