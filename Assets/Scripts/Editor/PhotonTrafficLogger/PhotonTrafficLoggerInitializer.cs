using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class PhotonTrafficLoggerInitializer : MonoBehaviour
{
    // 現状のインゲームシーン名
    private const string InGameSceneName = "InGameMock";
    private const string DevInGameSceneName = "InGameMock";

    private static PhotonTrafficLoggerInitializer _instance;
    private bool _hasInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInitialize()
    {
        // Create a persistent initializer object for runtime builds
        if (_instance == null)
        {
            var initializerObject = new GameObject("PhotonTrafficLoggerInitializer");
            _instance = initializerObject.AddComponent<PhotonTrafficLoggerInitializer>();
            DontDestroyOnLoad(initializerObject);

            // Listen for scene changes
            SceneManager.sceneLoaded += _instance.OnSceneLoaded;

            Debug.Log("[PhotonTrafficLoggerInitializer] Runtime initializer created");
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        // Check current scene on awake
        InitializeIfInGameScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeIfInGameScene();
    }

    public static void InitializeIfInGameScene()
    {
        var currentScene = SceneManager.GetActiveScene().name;
        if (IsInGameScene(currentScene))
        {
            InitializeTrafficLogger();
        }
    }

    public static bool IsInGameScene(string sceneName)
    {
        return sceneName == InGameSceneName || sceneName == DevInGameSceneName;
    }

    public static System.Type FindTypeByName(string typeName)
    {
        Debug.Log($"[PhotonTrafficLoggerInitializer] Searching for type: {typeName}");

        // Search through all loaded assemblies
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName || t.FullName == typeName ||
                                                                   t.FullName?.Contains(typeName) == true);
                if (type != null)
                {
                    Debug.Log(
                        $"[PhotonTrafficLoggerInitializer] Found type {typeName} -> {type.FullName} in assembly: {assembly.GetName().Name}");
                    return type;
                }
            }
            catch (System.Exception e)
            {
                // Skip assemblies that can't be reflected
                Debug.LogWarning(
                    $"[PhotonTrafficLoggerInitializer] Could not search assembly {assembly.GetName().Name}: {e.Message}");
            }
        }

        Debug.LogError($"[PhotonTrafficLoggerInitializer] Type {typeName} not found in any loaded assembly");

        // Debug: List all types containing "PhotonTraffic" for troubleshooting
        Debug.Log("[PhotonTrafficLoggerInitializer] Available PhotonTraffic types:");
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var photonTypes = assembly.GetTypes().Where(t =>
                    t.Name.Contains("PhotonTraffic") || t.FullName?.Contains("PhotonTraffic") == true);
                foreach (var type in photonTypes)
                {
                    Debug.Log($"  - {type.FullName} in {assembly.GetName().Name}");
                }
            }
            catch (System.Exception)
            {
                /* skip */
            }
        }

        return null;
    }

    public static void InitializeTrafficLogger()
    {
        Debug.Log("[PhotonTrafficLoggerInitializer] Starting traffic logger initialization");

        // Prevent multiple initialization
        if (_instance != null && _instance._hasInitialized)
        {
            Debug.Log("[PhotonTrafficLoggerInitializer] Already initialized, skipping");
            return;
        }

        // Try multiple ways to find the PhotonTrafficLoggerSettings type
        var settingsType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLoggerSettings") ??
                           System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLoggerSettings, Assembly-CSharp") ??
                           FindTypeByName("PhotonTrafficLoggerSettings");

        if (settingsType == null)
        {
            Debug.LogError("[PhotonTrafficLoggerInitializer] PhotonTrafficLoggerSettings type not found");
            return;
        }

        Debug.Log($"[PhotonTrafficLoggerInitializer] Found PhotonTrafficLoggerSettings type: {settingsType.FullName}");

        var settings = System.Activator.CreateInstance(settingsType);
        var loadMethod = settingsType.GetMethod("LoadFromPrefs");
        var enabledProperty = settingsType.GetProperty("Enabled");

        loadMethod?.Invoke(settings, null);

        var enabled = (bool)(enabledProperty?.GetValue(settings) ?? false);
        Debug.Log($"[PhotonTrafficLoggerInitializer] Settings loaded - Enabled: {enabled}");

        if (!enabled)
        {
            Debug.Log("[PhotonTrafficLoggerInitializer] Traffic logger is disabled in settings");
            return;
        }

        // Find or create the traffic logger
        var existingLoggerType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger") ??
                                 System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger, Assembly-CSharp") ??
                                 FindTypeByName("PhotonTrafficLogger");

        if (existingLoggerType == null)
        {
            Debug.LogError("[PhotonTrafficLoggerInitializer] PhotonTrafficLogger type not found");
            return;
        }

        Debug.Log($"[PhotonTrafficLoggerInitializer] Found PhotonTrafficLogger type: {existingLoggerType.FullName}");

        var existingLogger = Object.FindFirstObjectByType(existingLoggerType);
        if (existingLogger != null)
        {
            Debug.Log("[PhotonTrafficLoggerInitializer] Traffic logger already exists");
            return;
        }

        // Create a new GameObject for the traffic logger
        var loggerObject = new GameObject("PhotonTrafficLogger");
        var addedComponent = loggerObject.AddComponent(existingLoggerType);
        Debug.Log(
            $"[PhotonTrafficLoggerInitializer] Created PhotonTrafficLogger GameObject and added component: {addedComponent}");

        // Add runtime display component for development builds and editor
        var shouldAddDisplay = Debug.isDebugBuild || Application.isEditor || !Application.isEditor;  // Always add in builds
        Debug.Log($"[PhotonTrafficLoggerInitializer] Should add runtime display: {shouldAddDisplay} (Debug.isDebugBuild: {Debug.isDebugBuild}, Application.isEditor: {Application.isEditor})");
        
        if (shouldAddDisplay)
        {
            var displayType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLoggerRuntimeDisplay") ??
                              System.Type.GetType(
                                  "PhotonTrafficLogger.PhotonTrafficLoggerRuntimeDisplay, Assembly-CSharp") ??
                              FindTypeByName("PhotonTrafficLoggerRuntimeDisplay");
            if (displayType != null)
            {
                var displayComponent = loggerObject.AddComponent(displayType);
                Debug.Log($"[PhotonTrafficLoggerInitializer] Added runtime display component: {displayComponent}");
            }
            else
            {
                Debug.LogWarning("[PhotonTrafficLoggerInitializer] PhotonTrafficLoggerRuntimeDisplay type not found");
            }
        }

        // Make it persist across scenes
        Object.DontDestroyOnLoad(loggerObject);

        // Mark as initialized
        if (_instance != null)
        {
            _instance._hasInitialized = true;
        }

        Debug.Log(
            $"[PhotonTrafficLoggerInitializer] Initialized traffic logger in scene: {SceneManager.GetActiveScene().name}");
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}