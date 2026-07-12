using System.Collections.Generic;
using InGame.Common;
using InGame.Interact;
using InGame.Player;
using InGame.Player.Ability;
using InGame.Player.Ult;
using NaughtyAttributes;
using September.Common;
using UnityEngine;
using UnityEngine.Playables;
using InGame.Bot;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace September.InGame.Player.Data
{
    [CreateAssetMenu(fileName = "New CharacterData", menuName = "September/Character Data", order = 0)]
    public class CharacterData : ScriptableObject
    {
        [Header("基本設定")]
        [SerializeField] public CharacterType CharacterType = CharacterType.OkabeWright;
        [SerializeField] public GameObject TemplatePrefab;
        [SerializeField] public GameObject BotTemplatePrefab;
        [SerializeField] public GameObject GeneratedCharacterPrefab;
        [SerializeField] public string OutputPath;

        public string AssetPath => $"{OutputPath}/{name}.prefab";
        public string BotAssetPath => $"{OutputPath}/Bot {name}.prefab";

        // Root Object Components

        [Header("PlayerMovement")]
        [SerializeField] public float MoveSpeed;
        [SerializeField, Tooltip("地面と認識する最大角度")] public float GroundSlopeThreshold = 45f;
        [SerializeField] public LayerMask GroundLayer = ~0;
        [SerializeField] public float OgreMoveSpeed;
        [SerializeField] public float OgreDashSpeed;
        [SerializeField] public float DashSpeed;
        [SerializeField] public float DashCooldown = 3f;
        [SerializeField] public float StaminaConsumption;
        [SerializeField, Tooltip("degree/s")] public float RotationSpeed = 5f;
        [SerializeField, Tooltip("最大高さ")] public float MaxLedgeHeight;
        [SerializeField, Tooltip("最小高さ")] public float MinLedgeHeight;
        [SerializeField, Tooltip("最大奥行")] public float MaxLedgeDepth;
        [SerializeField] public float ReachDistance;
        [SerializeField] public float TimeToVault;
        [SerializeField] public AnimationCurve VaultCurve;

        [Header("PlayerAbilityManager")]
        [SerializeReference, SubclassSelector] private List<AbilityBase> _abilities = new();
        [SerializeReference, SubclassSelector] private List<IAbilityExecuteCondition> _conditions = new();

        [Header("AnimationClipPlayer")]
        [SerializeField] private List<LayerInfo> _layerInfo;
        [SerializeField] public AnimationClip Wait;
        [SerializeField] public AnimationClip Walk;
        [SerializeField] public AnimationClip Run;

        [Header("AnimationClipPlayerManager")]
        [SerializeField] public AnimationClip JumpOver;
        [SerializeField] public float JumpOverDuration = 0.2f;
        [SerializeField] public AnimationClip FallDown;
        [SerializeField] public AnimationClip Faint;
        [SerializeField] public AnimationClip GetUp;

        [Header("PlayerInteractionController")]
        [SerializeField] public float InteractRadius = 2.5f;
        [SerializeField] public LayerMask InteractMask;
        [SerializeField, Range(0f, 180f)] public float InteractAngle = 90f; // 前方180度
        [SerializeField] public float BaseInteractTime = 1.0f;
        [SerializeField] public float OgreInteractMultiplier = 1.0f;
        [SerializeField] public float InteractResponseTimeout = 3f;
        [SerializeField] public float InteractAngleBuffer = 10f; // 角度に+10°
        [SerializeField] public float InteractRadiusBuffer = 0.3f; // 距離に+0.3m

        [Header("PlayerEquipmentManager")]
        [SerializeField] public Equipment[] EquipmentData;

        [Header("UltCondition")]
        [SerializeField] public int RequireScore;

        [Header("PlayableDirector")]
        [SerializeField] public PlayableAsset UltSequence;


        // Mesh Object Components

        [Header("Animator")]
        [SerializeField] private AnimatorController _animatorController;
        [SerializeField] private Avatar _avatar;

        [Header("PlayerEffectController")]
        [SerializeField] public Vector3 StunEffectPositionOffset;

        [Header("PlayerAudioController")]
        [SerializeField] public string FootstepCueName;
        [SerializeField] public string PunchSwingCueName;
        [SerializeField] public string PunchHitCueName;
        [SerializeField] private List<AnimationClip> _footstepBlockClipList = new();


        // Others

        [Header("Mesh")]
        [SerializeField] public GameObject MeshFbx;

        [Header("ReadTargetPrefab")]
        [SerializeField] public GameObject ReadTargetPrefab;

#if UNITY_EDITOR
        [Button]
        public void CreateCharacterFromTemplate()
        {
            Undo.RecordObject(this, "Create Character Data");

            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath);
            if (existingPrefab)
            {
                AssetDatabase.DeleteAsset(AssetPath);
            }

            GameObject cloneCharacter = (GameObject)PrefabUtility.InstantiatePrefab(TemplatePrefab);

            OverwriteProperties(cloneCharacter);

            GeneratedCharacterPrefab = PrefabUtility.SaveAsPrefabAsset(cloneCharacter, AssetPath);

            DestroyImmediate(cloneCharacter);

            Debug.Log(AssetPath);
        }

        [Button]
        public void CreateBotCharacterFromTemplate()
        {
            Undo.RecordObject(this, "Create Character Data");

            if (!AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath))
            {
                Debug.LogWarning("元となるキャラクタープレハブが存在しないため、ボットの作成に失敗しました");
                return;
            }

            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BotAssetPath);
            if (existingPrefab)
            {
                AssetDatabase.DeleteAsset(BotAssetPath);
            }

            GameObject cloneCharacter = (GameObject)PrefabUtility.InstantiatePrefab(GeneratedCharacterPrefab);

            OverwriteProperties(cloneCharacter);

            AttachBotComponents(cloneCharacter);

            PrefabUtility.SaveAsPrefabAsset(cloneCharacter, BotAssetPath);

            DestroyImmediate(cloneCharacter);

            Debug.Log(BotAssetPath);
        }

        private void OverwriteProperties(GameObject cloneCharacter)
        {
            var playerMovement = cloneCharacter.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                var so = new SerializedObject(playerMovement);
                so.FindProperty("_moveSpeed").floatValue = MoveSpeed;
                so.FindProperty("_groundSlopeThreshold").floatValue = GroundSlopeThreshold;
                so.FindProperty("_groundLayer").intValue = GroundLayer;
                so.FindProperty("_ogreMoveSpeed").floatValue = OgreMoveSpeed;
                so.FindProperty("_ogreDashSpeed").floatValue = OgreDashSpeed;
                so.FindProperty("_dashSpeed").floatValue = DashSpeed;
                so.FindProperty("_dashCooldown").floatValue = DashCooldown;
                so.FindProperty("_staminaConsumption").floatValue = StaminaConsumption;
                so.FindProperty("_rotationSpeed").floatValue = RotationSpeed;
                so.FindProperty("_maxLedgeHeight").floatValue = MaxLedgeHeight;
                so.FindProperty("_minLedgeHeight").floatValue = MinLedgeHeight;
                so.FindProperty("_maxLedgeDepth").floatValue = MaxLedgeDepth;
                so.FindProperty("_reachDistance").floatValue = ReachDistance;
                so.FindProperty("_timeToVault").floatValue = TimeToVault;
                so.FindProperty("_vaultCurve").animationCurveValue = VaultCurve;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var playerInteractionController = cloneCharacter.GetComponent<PlayerInteractionController>();
            if (playerInteractionController != null)
            {
                var so = new SerializedObject(playerInteractionController);
                so.FindProperty("_interactRadius").floatValue = InteractRadius;
                so.FindProperty("_interactMask").intValue = InteractMask;
                so.FindProperty("_interactAngle").floatValue = InteractAngle;
                so.FindProperty("_baseInteractTime").floatValue = BaseInteractTime;
                so.FindProperty("_ogreInteractMultiplier").floatValue = OgreInteractMultiplier;
                so.FindProperty("_interactResponseTimeout").floatValue = InteractResponseTimeout;
                so.FindProperty("_interactAngleBuffer").floatValue = InteractAngleBuffer;
                so.FindProperty("_interactRadiusBuffer").floatValue = InteractRadiusBuffer;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var playerAbilityManager = cloneCharacter.GetComponent<PlayerAbilityManager>();
            if (playerAbilityManager != null)
            {
                var so = new SerializedObject(playerAbilityManager);

                so.FindProperty("_abilities").SetArrayManagedReferences(_abilities);
                so.FindProperty("_conditions").SetArrayManagedReferences(_conditions);

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var animationClipPlayer = cloneCharacter.GetComponent<AnimationClipPlayer>();
            if (animationClipPlayer != null)
            {
                var so = new SerializedObject(animationClipPlayer);

                so.FindProperty("_layerInfo").SetArrayBoxedValues(_layerInfo);
                so.FindProperty("_wait").objectReferenceValue = Wait;
                so.FindProperty("_walk").objectReferenceValue = Walk;
                so.FindProperty("_run").objectReferenceValue = Run;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var animationClipPlayerManager = cloneCharacter.GetComponent<AnimationClipPlayerManager>();
            if (animationClipPlayerManager != null)
            {
                var so = new SerializedObject(animationClipPlayerManager);
                so.FindProperty("_jumpOver").objectReferenceValue = JumpOver;
                so.FindProperty("_jumpOverDuration").floatValue = JumpOverDuration;
                so.FindProperty("_fallDown").objectReferenceValue = FallDown;
                so.FindProperty("_faint").objectReferenceValue = Faint;
                so.FindProperty("_getUp").objectReferenceValue = GetUp;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var playerEquipmentManager = cloneCharacter.GetComponent<PlayerEquipmentManager>();
            if (playerEquipmentManager != null)
            {
                var so = new SerializedObject(playerEquipmentManager);
                so.FindProperty("_equipmentData").SetArrayBoxedValues(EquipmentData);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var ultCondition = cloneCharacter.GetComponent<UltCondition>();
            if (ultCondition != null)
            {
                var so = new SerializedObject(ultCondition);
                so.FindProperty("_requiredScore").intValue = RequireScore;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var playableDirector = cloneCharacter.GetComponent<PlayableDirector>();
            if (playableDirector != null)
            {
                var so = new SerializedObject(playableDirector);
                so.FindProperty("m_PlayableAsset").objectReferenceValue = UltSequence;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject meshObject = null;

            foreach (Transform child in cloneCharacter.transform)
            {
                if (child.name == "Mesh")
                {
                    meshObject = child.gameObject;
                    break;
                }
            }

            if (meshObject != null)
            {
                {
                    for (int i = meshObject.transform.childCount - 1; i >= 0; i--)
                    {
                        DestroyImmediate(meshObject.transform.GetChild(i).gameObject);
                    }
                }

                {
                    GameObject newMesh = Instantiate(MeshFbx, meshObject.transform);

                    var renderers = newMesh.GetComponentsInChildren<Renderer>();
                    var playerRenderer = cloneCharacter.GetComponent<PlayerRenderer>();
                    {
                        var so = new SerializedObject(playerRenderer);
                        so.FindProperty("_renderers").SetArrayBoxedValues(renderers);
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }

                    for (int i = newMesh.transform.childCount - 1; i >= 0; i--)
                    {
                        var child = newMesh.transform.GetChild(i);
                        child.parent = meshObject.transform;
                    }

                    DestroyImmediate(newMesh);
                }

                var animator = meshObject.GetComponent<Animator>();
                if (animator != null)
                {
                    var so = new SerializedObject(animator);
                    so.FindProperty("m_Controller").objectReferenceValue = _animatorController;
                    so.FindProperty("m_Avatar").objectReferenceValue = _avatar;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                var playerEffectController = meshObject.GetComponent<PlayerEffectController>();
                if (playerEffectController != null)
                {
                    var so = new SerializedObject(playerEffectController);
                    so.FindProperty("_stunEffectPositionOffset").vector3Value = StunEffectPositionOffset;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                var playerAudioController = meshObject.GetComponent<PlayerAudioController>();
                if (playerAudioController != null)
                {
                    var so = new SerializedObject(playerAudioController);
                    so.FindProperty("_footstepCueName").stringValue = FootstepCueName;
                    so.FindProperty("_punchSwingCueName").stringValue = PunchSwingCueName;
                    so.FindProperty("_punchHitCueName").stringValue = PunchHitCueName;
                    so.FindProperty("_footstepBlockClipList").SetArrayBoxedValues(_footstepBlockClipList);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                Debug.LogWarning("Meshオブジェクトが見つかりません");
            }
        }

        private void AttachBotComponents(GameObject cloneCharacter)
        {
            var srcBotManager = BotTemplatePrefab.GetComponent<BotManager>();
            var srcBotStateMachine = BotTemplatePrefab.GetComponent<BotStateMachine>();
            var srcBotInputManager = BotTemplatePrefab.GetComponent<BotInputManager>();

            var dstBotManager = SerializedObjectExtensions.PasteComponentAsNew(srcBotManager, cloneCharacter);
            var dstBotStateMachine = SerializedObjectExtensions.PasteComponentAsNew(srcBotStateMachine, cloneCharacter);
            var dstBotInputManager = SerializedObjectExtensions.PasteComponentAsNew(srcBotInputManager, cloneCharacter);

            SerializedObjectExtensions.ReplaceReferencesGlobalToLocal(dstBotManager, srcBotManager);
            SerializedObjectExtensions.ReplaceReferencesGlobalToLocal(dstBotStateMachine, srcBotStateMachine);
            SerializedObjectExtensions.ReplaceReferencesGlobalToLocal(dstBotInputManager, srcBotInputManager);

            DestroyImmediate(cloneCharacter.GetComponent<PlayerManager>());
            DestroyImmediate(cloneCharacter.GetComponent<PlayerInputManager>());
        }

        /// <summary>
        /// TemplatePrefabのコンポーネントが持つデータを読み取って自身のフィールドに書き込む処理
        /// </summary>

        [Button]
        public void ReadPropertiesFromTemplate()
        {
            Undo.RecordObject(this, "Read Character Data");

            GameObject cloneCharacter = (GameObject)PrefabUtility.InstantiatePrefab(ReadTargetPrefab);

            var playerMovement = cloneCharacter.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                var so = new SerializedObject(playerMovement);
                MoveSpeed = so.FindProperty("_moveSpeed").floatValue;
                GroundSlopeThreshold = so.FindProperty("_groundSlopeThreshold").floatValue;
                GroundLayer = so.FindProperty("_groundLayer").intValue;
                OgreMoveSpeed = so.FindProperty("_ogreMoveSpeed").floatValue;
                OgreDashSpeed = so.FindProperty("_ogreDashSpeed").floatValue;
                DashSpeed = so.FindProperty("_dashSpeed").floatValue;
                DashCooldown = so.FindProperty("_dashCooldown").floatValue;
                StaminaConsumption = so.FindProperty("_staminaConsumption").floatValue;
                RotationSpeed = so.FindProperty("_rotationSpeed").floatValue;
                MaxLedgeHeight = so.FindProperty("_maxLedgeHeight").floatValue;
                MinLedgeHeight = so.FindProperty("_minLedgeHeight").floatValue;
                MaxLedgeDepth = so.FindProperty("_maxLedgeDepth").floatValue;
                ReachDistance = so.FindProperty("_reachDistance").floatValue;
                TimeToVault = so.FindProperty("_timeToVault").floatValue;
                VaultCurve = so.FindProperty("_vaultCurve").animationCurveValue;
            }

            var playerInteractionController = cloneCharacter.GetComponent<PlayerInteractionController>();
            if (playerInteractionController != null)
            {
                var so = new SerializedObject(playerInteractionController);
                InteractRadius = so.FindProperty("_interactRadius").floatValue;
                InteractMask = so.FindProperty("_interactMask").intValue;
                InteractAngle = so.FindProperty("_interactAngle").floatValue;
                BaseInteractTime = so.FindProperty("_baseInteractTime").floatValue;
                OgreInteractMultiplier = so.FindProperty("_ogreInteractMultiplier").floatValue;
                InteractResponseTimeout = so.FindProperty("_interactResponseTimeout").floatValue;
                InteractAngleBuffer = so.FindProperty("_interactAngleBuffer").floatValue;
                InteractRadiusBuffer = so.FindProperty("_interactRadiusBuffer").floatValue;
            }

            var playerAbilityManager = cloneCharacter.GetComponent<PlayerAbilityManager>();
            if (playerAbilityManager != null)
            {
                var so = new SerializedObject(playerAbilityManager);
                
                var abilitiesProp = so.FindProperty("_abilities");
                _abilities = new List<AbilityBase>();
                for (int i = 0; i < abilitiesProp.arraySize; i++)
                {
                    _abilities.Add(abilitiesProp.GetArrayElementAtIndex(i).managedReferenceValue as AbilityBase);
                }

                var conditionsProp = so.FindProperty("_conditions");
                _conditions = new List<IAbilityExecuteCondition>();
                for (int i = 0; i < conditionsProp.arraySize; i++)
                {
                    _conditions.Add(conditionsProp.GetArrayElementAtIndex(i).managedReferenceValue as IAbilityExecuteCondition);
                }
            }

            var animationClipPlayer = cloneCharacter.GetComponent<AnimationClipPlayer>();
            if (animationClipPlayer != null)
            {
                var so = new SerializedObject(animationClipPlayer);
                
                var field = typeof(AnimationClipPlayer).GetField("_layerInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var val = field.GetValue(animationClipPlayer) as List<LayerInfo>;
                    _layerInfo = val != null ? new List<LayerInfo>(val) : new List<LayerInfo>();
                }

                Wait = so.FindProperty("_wait").objectReferenceValue as AnimationClip;
                Walk = so.FindProperty("_walk").objectReferenceValue as AnimationClip;
                Run = so.FindProperty("_run").objectReferenceValue as AnimationClip;
            }

            var animationClipPlayerManager = cloneCharacter.GetComponent<AnimationClipPlayerManager>();
            if (animationClipPlayerManager != null)
            {
                var so = new SerializedObject(animationClipPlayerManager);
                JumpOver = so.FindProperty("_jumpOver").objectReferenceValue as AnimationClip;
                JumpOverDuration = so.FindProperty("_jumpOverDuration").floatValue;
                FallDown = so.FindProperty("_fallDown").objectReferenceValue as AnimationClip;
                Faint = so.FindProperty("_faint").objectReferenceValue as AnimationClip;
                GetUp = so.FindProperty("_getUp").objectReferenceValue as AnimationClip;
            }

            var playerEquipmentManager = cloneCharacter.GetComponent<PlayerEquipmentManager>();
            if (playerEquipmentManager != null)
            {
                var field = typeof(PlayerEquipmentManager).GetField("_equipmentData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    EquipmentData = field.GetValue(playerEquipmentManager) as Equipment[];
                }
            }

            var ultCondition = cloneCharacter.GetComponent<UltCondition>();
            if (ultCondition != null)
            {
                var so = new SerializedObject(ultCondition);
                RequireScore = so.FindProperty("_requiredScore").intValue;
            }

            var playableDirector = cloneCharacter.GetComponent<PlayableDirector>();
            if (playableDirector != null)
            {
                var so = new SerializedObject(playableDirector);
                UltSequence = so.FindProperty("m_PlayableAsset").objectReferenceValue as PlayableAsset;
            }

            GameObject meshObject = null;

            foreach (Transform child in cloneCharacter.transform)
            {
                if (child.name == "Mesh")
                {
                    meshObject = child.gameObject;
                    break;
                }
            }

            if (meshObject != null)
            {
                meshObject.GetComponent<Animator>().applyRootMotion = true;

                var animator = meshObject.GetComponent<Animator>();
                if (animator != null)
                {
                    var so = new SerializedObject(animator);
                    _animatorController = so.FindProperty("m_Controller").objectReferenceValue as AnimatorController;
                    _avatar = so.FindProperty("m_Avatar").objectReferenceValue as Avatar;
                }

                var playerEffectController = meshObject.GetComponent<PlayerEffectController>();
                if (playerEffectController != null)
                {
                    var so = new SerializedObject(playerEffectController);
                    StunEffectPositionOffset = so.FindProperty("_stunEffectPositionOffset").vector3Value;
                }

                var playerAudioController = meshObject.GetComponent<PlayerAudioController>();
                if (playerAudioController != null)
                {
                    var so = new SerializedObject(playerAudioController);
                    FootstepCueName = so.FindProperty("_footstepCueName").stringValue;
                    PunchSwingCueName = so.FindProperty("_punchSwingCueName").stringValue;
                    PunchHitCueName = so.FindProperty("_punchHitCueName").stringValue;

                    var field = typeof(PlayerAudioController).GetField("_footstepBlockClipList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var val = field.GetValue(playerAudioController) as List<AnimationClip>;
                        _footstepBlockClipList = val != null ? new List<AnimationClip>(val) : new List<AnimationClip>();
                    }
                }
            }
            else
            {
                Debug.LogWarning("Meshオブジェクトが見つかりません");
            }

            EditorUtility.SetDirty(this);

            DestroyImmediate(cloneCharacter);

            Debug.Log(AssetPath);
        }
#endif
    }
}
