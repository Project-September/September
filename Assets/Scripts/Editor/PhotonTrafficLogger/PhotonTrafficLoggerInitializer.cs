using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Statistics;
using System.Linq;

[InitializeOnLoad]
public class PhotonTrafficLoggerInitializer
{
    // 現状のインゲームシーン名
    private const string InGameSceneName = "InGameMock";
    private const string DevInGameSceneName = "InGameMock";

    static PhotonTrafficLoggerInitializer()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Check if we're in an ingame scene
            var currentScene = SceneManager.GetActiveScene().name;
            if (IsInGameScene(currentScene))
            {
                InitializeTrafficLogger();
            }
            else
            {
                // Listen for scene changes to detect when we enter an ingame scene
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsInGameScene(scene.name))
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
                    Debug.Log($"[PhotonTrafficLoggerInitializer] Found type {typeName} -> {type.FullName} in assembly: {assembly.GetName().Name}");
                    return type;
                }
            }
            catch (System.Exception e)
            {
                // Skip assemblies that can't be reflected
                Debug.LogWarning($"[PhotonTrafficLoggerInitializer] Could not search assembly {assembly.GetName().Name}: {e.Message}");
            }
        }
        
        Debug.LogError($"[PhotonTrafficLoggerInitializer] Type {typeName} not found in any loaded assembly");
        
        // Debug: List all types containing "PhotonTraffic" for troubleshooting
        Debug.Log("[PhotonTrafficLoggerInitializer] Available PhotonTraffic types:");
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var photonTypes = assembly.GetTypes().Where(t => t.Name.Contains("PhotonTraffic") || t.FullName?.Contains("PhotonTraffic") == true);
                foreach (var type in photonTypes)
                {
                    Debug.Log($"  - {type.FullName} in {assembly.GetName().Name}");
                }
            }
            catch (System.Exception) { /* skip */ }
        }
        
        return null;
    }

    public static void InitializeTrafficLogger()
    {
        Debug.Log("[PhotonTrafficLoggerInitializer] Starting traffic logger initialization");
        
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
        Debug.Log($"[PhotonTrafficLoggerInitializer] Created PhotonTrafficLogger GameObject and added component: {addedComponent}");

        // Add runtime display component for development builds
        if (Debug.isDebugBuild)
        {
            var displayType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLoggerRuntimeDisplay") ??
                             System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLoggerRuntimeDisplay, Assembly-CSharp") ??
                             FindTypeByName("PhotonTrafficLoggerRuntimeDisplay");
            if (displayType != null)
            {
                loggerObject.AddComponent(displayType);
            }
        }

        // Make it persist across scenes
        Object.DontDestroyOnLoad(loggerObject);

        Debug.Log(
            $"[PhotonTrafficLoggerInitializer] Initialized traffic logger in scene: {SceneManager.GetActiveScene().name}");
    }
}

public class PhotonTrafficLoggerMenuItem
{
    [MenuItem("September/Photon Traffic Logger/Start Logging")]
    public static void StartLogging()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("PhotonTrafficLogger can only be used in Play mode.");
            return;
        }

        // Try to find existing logger
        var loggerType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger") ??
                        System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger, Assembly-CSharp") ??
                        PhotonTrafficLoggerInitializer.FindTypeByName("PhotonTrafficLogger");
        var logger = loggerType != null ? Object.FindFirstObjectByType(loggerType) : null;
        
        if (logger == null)
        {
            // Check if we're in an ingame scene, if not, create logger manually
            var currentScene = SceneManager.GetActiveScene().name;
            if (PhotonTrafficLoggerInitializer.IsInGameScene(currentScene))
            {
                // Force initialize if in ingame scene
                PhotonTrafficLoggerInitializer.InitializeTrafficLogger();
                logger = loggerType != null ? Object.FindFirstObjectByType(loggerType) : null;
            }
            else
            {
                Debug.LogWarning("PhotonTrafficLogger is only available in InGame scenes (InGameMock). Current scene: " + currentScene);
                return;
            }
        }

        if (logger != null)
        {
            var startMethod = logger.GetType().GetMethod("StartLogging");
            if (startMethod != null)
            {
                startMethod.Invoke(logger, null);
                Debug.Log("Started Photon traffic logging");
            }
        }
        else
        {
            Debug.LogWarning("PhotonTrafficLogger not found. Make sure it's enabled in the settings and you're in an InGame scene.");
        }
    }

    [MenuItem("September/Photon Traffic Logger/Stop Logging")]
    public static void StopLogging()
    {
        var loggerType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger") ??
                        System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger, Assembly-CSharp") ??
                        PhotonTrafficLoggerInitializer.FindTypeByName("PhotonTrafficLogger");
        var logger = loggerType != null ? Object.FindFirstObjectByType(loggerType) : null;
        if (logger != null)
        {
            var stopMethod = logger.GetType().GetMethod("StopLogging");
            if (stopMethod != null)
            {
                stopMethod.Invoke(logger, null);
                Debug.Log("Stopped Photon traffic logging");
            }
        }
        else
        {
            Debug.LogWarning("PhotonTrafficLogger not found.");
        }
    }

    [MenuItem("September/Photon Traffic Logger/Clear Logs")]
    public static void ClearLogs()
    {
        var loggerType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger") ??
                        System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger, Assembly-CSharp") ??
                        PhotonTrafficLoggerInitializer.FindTypeByName("PhotonTrafficLogger");
        var logger = loggerType != null ? Object.FindFirstObjectByType(loggerType) : null;
        if (logger != null)
        {
            var clearMethod = logger.GetType().GetMethod("ClearLogs");
            if (clearMethod != null)
            {
                clearMethod.Invoke(logger, null);
                Debug.Log("Cleared Photon traffic logs");
            }
        }
        else
        {
            Debug.LogWarning("PhotonTrafficLogger not found.");
        }
    }

    [MenuItem("September/Photon Traffic Logger/Attach FusionStats to NetworkRunner")]
    public static void AttachFusionStatsToNetworkRunner()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("This operation can only be performed in Play mode.");
            return;
        }

        var networkRunners = Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        int attachedCount = 0;

        foreach (var networkRunner in networkRunners)
        {
            if (networkRunner.GetComponent<FusionStatistics>() == null)
            {
                networkRunner.gameObject.AddComponent<FusionStatistics>();
                attachedCount++;
            }
        }

        Debug.Log($"Attached FusionStatistics to {attachedCount} NetworkRunners (Total: {networkRunners.Length})");
    }

    [MenuItem("September/Photon Traffic Logger/Force Initialize Logger")]
    public static void ForceInitializeLogger()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("This operation can only be performed in Play mode.");
            return;
        }

        var loggerType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger");
        var existingLogger = loggerType != null ? Object.FindFirstObjectByType(loggerType) : null;
        
        if (existingLogger != null)
        {
            Debug.LogWarning("PhotonTrafficLogger already exists.");
            return;
        }

        // Force create logger regardless of scene or settings
        var loggerObject = new GameObject("PhotonTrafficLogger");
        if (loggerType != null)
        {
            loggerObject.AddComponent(loggerType);
        }

        // Add runtime display component for development builds
        if (Debug.isDebugBuild)
        {
            var displayType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLoggerRuntimeDisplay") ??
                             System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLoggerRuntimeDisplay, Assembly-CSharp") ??
                             PhotonTrafficLoggerInitializer.FindTypeByName("PhotonTrafficLoggerRuntimeDisplay");
            if (displayType != null)
            {
                loggerObject.AddComponent(displayType);
            }
        }

        // Make it persist across scenes
        Object.DontDestroyOnLoad(loggerObject);

        Debug.Log($"[PhotonTrafficLoggerInitializer] Force initialized traffic logger in scene: {SceneManager.GetActiveScene().name}");
    }

    [MenuItem("September/Photon Traffic Logger/Check Logger Status")]
    public static void CheckLoggerStatus()
    {
        var loggerType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger") ??
                        System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger, Assembly-CSharp") ??
                        PhotonTrafficLoggerInitializer.FindTypeByName("PhotonTrafficLogger");
        var logger = loggerType != null ? Object.FindFirstObjectByType(loggerType) : null;
        
        if (logger != null)
        {
            var isLoggingProperty = logger.GetType().GetProperty("IsLogging");
            var isLogging = isLoggingProperty?.GetValue(logger);
            Debug.Log($"PhotonTrafficLogger found. IsLogging: {isLogging}");
        }
        else
        {
            Debug.LogWarning("PhotonTrafficLogger not found.");
        }

        // Check settings
        var settingsType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLoggerSettings") ??
                          System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLoggerSettings, Assembly-CSharp") ??
                          PhotonTrafficLoggerInitializer.FindTypeByName("PhotonTrafficLoggerSettings");
        if (settingsType != null)
        {
            var settings = System.Activator.CreateInstance(settingsType);
            var loadMethod = settingsType.GetMethod("LoadFromPrefs");
            var enabledProperty = settingsType.GetProperty("Enabled");

            loadMethod?.Invoke(settings, null);
            var enabled = (bool)(enabledProperty?.GetValue(settings) ?? false);
            
            Debug.Log($"PhotonTrafficLogger Settings - Enabled: {enabled}");
        }
        else
        {
            Debug.LogError("PhotonTrafficLoggerSettings type not found.");
        }
        
        Debug.Log($"Current Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Is InGame Scene: {PhotonTrafficLoggerInitializer.IsInGameScene(SceneManager.GetActiveScene().name)}");
    }
}