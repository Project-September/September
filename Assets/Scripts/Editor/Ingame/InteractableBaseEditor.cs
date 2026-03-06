#if UNITY_EDITOR
using InGame.Interact;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractableBase))]
public class InteractableBaseEditor : UnityEditor.Editor
{
    private GameObject _previewInstance;
    private ParticleSystem[] _particleSystems;
    private EffectType _currentEffectType;
    private double _lastTime;

    private SerializedProperty _cooldownEffectTransform;
    private SerializedProperty _cooldownEffectOffset;
    private SerializedProperty _cooldownEffectRotation;
    private SerializedProperty _cooldownEffectScale;
    private SerializedProperty _cooldownEffectType;

    private void OnEnable()
    {
        _cooldownEffectTransform = serializedObject.FindProperty("_cooldownEffectTransform");
        _cooldownEffectOffset = serializedObject.FindProperty("_cooldownEffectOffset");
        _cooldownEffectRotation = serializedObject.FindProperty("_cooldownEffectRotation");
        _cooldownEffectScale = serializedObject.FindProperty("_cooldownEffectScale");
        _cooldownEffectType = serializedObject.FindProperty("_cooldownEffectType");

        CreatePreview();
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        DestroyPreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        var newEffectType = (EffectType)_cooldownEffectType.enumValueIndex;
        if (_previewInstance == null || newEffectType != _currentEffectType)
        {
            DestroyPreview();
            CreatePreview();
        }

        UpdatePreviewTransform();
    }

    private void OnSceneGUI()
    {
        UpdatePreviewTransform();
    }

    private void OnEditorUpdate()
    {
        if (_particleSystems == null || _particleSystems.Length == 0) return;

        var currentTime = EditorApplication.timeSinceStartup;
        var deltaTime = (float)(currentTime - _lastTime);
        _lastTime = currentTime;

        foreach (var ps in _particleSystems)
        {
            if (ps == null) continue;
            ps.Simulate(deltaTime, false, false);
        }

        SceneView.RepaintAll();
    }

    private void CreatePreview()
    {
        DestroyPreview();

        var prefab = GetEffectPrefab();
        if (prefab == null) return;

        _currentEffectType = (EffectType)_cooldownEffectType.enumValueIndex;
        _previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        _previewInstance.name = "[Preview] CooldownEffect";
        _previewInstance.hideFlags = HideFlags.HideAndDontSave;

        DisableRuntimeComponents(_previewInstance);
        UpdatePreviewTransform();

        _particleSystems = _previewInstance.GetComponentsInChildren<ParticleSystem>(true);
        _lastTime = EditorApplication.timeSinceStartup;

        foreach (var ps in _particleSystems)
        {
            var main = ps.main;
            main.simulationSpeed = 1f;
            ps.Play();
        }
    }

    private void DestroyPreview()
    {
        _particleSystems = null;
        if (_previewInstance != null)
        {
            DestroyImmediate(_previewInstance);
            _previewInstance = null;
        }
    }

    private GameObject GetEffectPrefab()
    {
        var effectType = (EffectType)_cooldownEffectType.enumValueIndex;
        var db = Resources.Load<EffectDatabase>("EffectDatabase");
        if (db == null) return null;

        var data = db.GetEffectData(effectType);
        return data.Prefab;
    }

    private void UpdatePreviewTransform()
    {
        if (_previewInstance == null) return;

        var targetComponent = (InteractableBase)target;
        var baseTransform = _cooldownEffectTransform.objectReferenceValue as Transform;
        if (baseTransform == null) baseTransform = targetComponent.transform;

        _previewInstance.transform.position = baseTransform.position + _cooldownEffectOffset.vector3Value;
        _previewInstance.transform.rotation = Quaternion.Euler(_cooldownEffectRotation.vector3Value);
        _previewInstance.transform.localScale = _cooldownEffectScale.vector3Value;
    }

    private static void DisableRuntimeComponents(GameObject obj)
    {
        foreach (var component in obj.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            if (component is Transform) continue;
            if (component is Renderer) continue;
            if (component is MeshFilter) continue;
            if (component is ParticleSystem) continue;
            if (component is ParticleSystemRenderer) continue;

            if (component is Behaviour behaviour)
            {
                behaviour.enabled = false;
            }
        }
    }
}
#endif
