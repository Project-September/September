using System.Collections.Generic;
using InGame.Common;
using InGame.Interact;
using InGame.Player;
using InGame.Player.Ability;
using InGame.Player.Ult;
using NaughtyAttributes;
using September.Common;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;

namespace September.InGame.Player.Data
{
    [CreateAssetMenu(fileName = "New CharacterData", menuName = "September/Character Data", order = 0)]
    public class CharacterData : ScriptableObject
    {
        [Header("基本設定")]
        [SerializeField] public CharacterType CharacterType = CharacterType.OkabeWright;
        [SerializeField] public GameObject TemplatePrefab;
        [SerializeField] public string OutputPath;


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

        /// <summary> 管理するアビリティのリスト（InspectorでSubclassSelectorで選択）</summary>
        [Header("PlayerAbilityManager")]
        [SerializeReference, SubclassSelector] private List<AbilityBase> _abilities = new();
        /// <summary> アビリティ実行条件のリスト（InspectorでSubclassSelectorで選択）</summary>
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
        [SerializeField] public Transform InteractOrigin;
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
        [SerializeField] public ParticleSystem PunchEffect;
        [SerializeField] public Vector3 StunEffectPositionOffset;

        [Header("PlayerAudioController")]
        [SerializeField] public string FootstepCueName;
        [SerializeField] public string PunchSwingCueName;
        [SerializeField] public string PunchHitCueName;
        [SerializeField] private List<AnimationClip> _footstepBlockClipList = new();


        // Others

        [Header("Mesh")]
        [SerializeField] public GameObject MeshFbx;

        [Button]
        public void CreateCharacterFromTemplate()
        {
            Undo.RecordObject(this, "Create Character Data");

            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPath);
            if (existingPrefab)
            {
                AssetDatabase.DeleteAsset(OutputPath);
            }

            GameObject cloneCharacter = (GameObject)PrefabUtility.InstantiatePrefab(TemplatePrefab);

            var playerMovement = cloneCharacter.GetComponent<PlayerMovement>();

            var playerInteractionController = cloneCharacter.GetComponent<PlayerInteractionController>();

            var playerAbilityManager = cloneCharacter.GetComponent<PlayerAbilityManager>();

            var animationClipPlayer = cloneCharacter.GetComponent<AnimationClipPlayer>();

            var animationClipPlayerManager = cloneCharacter.GetComponent<AnimationClipPlayerManager>();

            var playerEquipmentManager = cloneCharacter.GetComponent<PlayerEquipmentManager>();

            var ultCondition = cloneCharacter.GetComponent<UltCondition>();

            var playableDirector = cloneCharacter.GetComponent<PlayableDirector>();


            GameObject meshObject = null;

            foreach (Transform child in cloneCharacter.transform)
            {
                if (child.name == "Mesh")
                {
                    meshObject = child.gameObject;
                    break;
                }
            }

            if (meshObject == null)
            {
                Debug.LogWarning("Meshオブジェクトが見つかりません");
                return;
            }

            meshObject.GetComponent<Animator>().applyRootMotion = true;

            var animator = meshObject.GetComponent<Animator>();

            var playerEffectController = meshObject.GetComponent<PlayerEffectController>();

            var playerAudioController = meshObject.GetComponent<PlayerAudioController>();

            PrefabUtility.SaveAsPrefabAsset(cloneCharacter, OutputPath);

            DestroyImmediate(cloneCharacter);

            Debug.Log(OutputPath);
        }
    }
}
