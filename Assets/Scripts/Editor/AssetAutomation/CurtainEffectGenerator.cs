using System.Collections.Generic;
using System.IO;
using System.Linq;
using September.NewResult;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Editor.AssetAutomation
{
    internal static class CurtainEffectGenerator
    {
        [MenuItem("Tools/AssetGenerators/連番画像からシーン遷移演出プレハブの生成")]
        private static void CreateFromSubFolders()
        {
            var spriteRoot = SelectAssetFolder("");

            if (string.IsNullOrEmpty(spriteRoot))
                return;

            var prefix = Path.GetFileName(spriteRoot);
        
            var controllerPath = spriteRoot + "/" + prefix + ".controller";
            var prefabPath = spriteRoot + "/" + prefix + ".prefab";
        
            const string templatePath = "Assets/DriveData/Templates/Curtain/Clip.anim";
            const string templatePrefabPath = "Assets/DriveData/Templates/Curtain/Curtain.prefab";

            var clipNames = new[]
            {
                "Close",
                "Close_Covered",
                "Hold",
                "Open_Covered",
                "Open"
            };

            var template = AssetDatabase.LoadAssetAtPath<AnimationClip>(templatePath);
            if (template == null)
            {
                Debug.LogError("Template AnimationClip not found");
                return;
            }

            if (!AssetDatabase.IsValidFolder(spriteRoot))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }
        
            // 既存のAnimatorControllerを削除
            var existingController = AssetDatabase.LoadAssetAtPath<AnimationClip>(controllerPath);
            if (existingController)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }
        
            // AnimatorController 作成
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            var stateMachine = controller.layers[0].stateMachine;

            // 直下のサブフォルダ取得
            string[] subFolders = AssetDatabase.GetSubFolders(spriteRoot);
            var createdClips = new List<AnimationClip>
            {
                new(){name = "Before Start"}
            };

            for (var i = 0; i < subFolders.Length; i++)
            {
                var folder = subFolders[i];
                string clipName = prefix + "_" + clipNames[i];
                Sprite[] sprites = LoadSprites(folder);

                if (sprites.Length == 0)
                {
                    Debug.LogWarning($"No sprites in {folder}");
                    continue;
                }

                AnimationClip clip = Object.Instantiate(template);
                clip.name = clipName;
                clip.frameRate = template.frameRate;

                ApplySpriteKeys(clip, sprites);

                string assetPath = $"{spriteRoot}/{clipName}.anim";

                // 既存のAnimationClipを削除
                var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (existingClip)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                AssetDatabase.CreateAsset(clip, assetPath);
                createdClips.Add(clip);
            }

            // ステート配置
            createdClips.Add(new(){name = "End"});
        
            int index = 0;
            foreach (var clip in createdClips)
            {
                Vector3 pos; 
                if (index == 0) pos =  new Vector3(30, 175, 0);
                else if (index != createdClips.Count - 1) pos = new Vector3(300, 50 * index, 0);
                else pos = new Vector3(30, 225, 0);
            
                var state = stateMachine.AddState(
                    clip.name,
                    pos
                );
                state.motion = clip;

                index++;
            }

            CurtainPrefabGenerator.CreatePrefabFromTemplate(templatePrefabPath, prefabPath, controller, createdClips.Select(x => x.name).ToList());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    
        static string SelectAssetFolder(string title)
        {
            string fullPath = EditorUtility.OpenFolderPanel(
                title,
                Application.dataPath,
                ""
            );

            if (string.IsNullOrEmpty(fullPath))
                return null;

            // Assets 配下であることを保証
            if (!fullPath.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Folder",
                    "Assets フォルダ配下を選択してください",
                    "OK"
                );
                return null;
            }

            // Assets 相対パスへ変換
            return "Assets" + fullPath.Substring(Application.dataPath.Length);
        }

        static Sprite[] LoadSprites(string folder)
        {
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });

            // 名前順で並べる（超重要）
            return guids
                .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(
                    AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(s => s.name)
                .ToArray();
        }

        static void ApplySpriteKeys(AnimationClip clip, Sprite[] sprites)
        {
            var binding = new EditorCurveBinding
            {
                type = typeof(Image),
                path = "",
                propertyName = "m_Sprite"
            };

            float frameRate = clip.frameRate;
            var keys = new ObjectReferenceKeyframe[sprites.Length];

            for (int i = 0; i < sprites.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / frameRate,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        }
    }

    internal static class CurtainPrefabGenerator
    {
        public static void CreatePrefabFromTemplate(
            string templatePrefabPath,
            string outputPrefabPath,
            AnimatorController controller,
            List<string> stateNames
        )
        {
            // テンプレPrefabをロード（編集用）
            GameObject prefabRoot =
                PrefabUtility.LoadPrefabContents(templatePrefabPath);

            try
            {
                // Animator に Controller を設定
                prefabRoot.transform.GetChild(0).GetChild(0)
                    .GetComponent<Animator>()
                    .runtimeAnimatorController = controller;

                ApplyStateNamesToPrefab(prefabRoot, stateNames);

                // Prefab 保存
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, outputPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
        
        private static void ApplyStateNamesToPrefab(
            GameObject prefabRoot,
            List<string> stateNames
        )
        {
            var holder = prefabRoot.GetComponent<AnimationSceneTransition>();
            if (holder == null) return;

            SerializedObject so = new SerializedObject(holder);

            string[] fieldNames =
            {
                "_defaultStateName",
                "_closingStateName",
                "_closingCoveredStateName",
                "_holdStateName",
                "_openingCoveredStateName",
                "_openingStateName",
                "_completedStateName",
            };

            for (int i = 0; i < fieldNames.Length; i++)
            {
                SerializedProperty prop = so.FindProperty(fieldNames[i]);
                if (prop == null) continue;

                prop.stringValue = i < stateNames.Count
                    ? stateNames[i]
                    : string.Empty;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}