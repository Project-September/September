using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Statistics;

[InitializeOnLoad]
public class PhotonTrafficLoggerInitializer
{
    // 現状のインゲームシーン名
    private const string INGAME_SCENE_NAME = "InGameMock";
    private const string DEV_INGAME_SCENE_NAME = "InGameMock";

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

    private static bool IsInGameScene(string sceneName)
    {
        return sceneName == INGAME_SCENE_NAME || sceneName == DEV_INGAME_SCENE_NAME;
    }

    private static void InitializeTrafficLogger()
    {
        var settingsType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLoggerSettings");
        if (settingsType == null) return;

        var settings = System.Activator.CreateInstance(settingsType);
        var loadMethod = settingsType.GetMethod("LoadFromPrefs");
        var enabledProperty = settingsType.GetProperty("Enabled");

        loadMethod?.Invoke(settings, null);

        var enabled = (bool)(enabledProperty?.GetValue(settings) ?? false);

        if (!enabled)
            return;

        // Find or create the traffic logger
        var existingLoggerType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger");
        var existingLogger = existingLoggerType != null ? Object.FindFirstObjectByType(existingLoggerType) : null;
        if (existingLogger != null)
            return;

        // Create a new GameObject for the traffic logger
        var loggerObject = new GameObject("PhotonTrafficLogger");
        if (existingLoggerType != null)
        {
            loggerObject.AddComponent(existingLoggerType);
        }

        // Add runtime display component for development builds
        if (Debug.isDebugBuild)
        {
            var displayType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLoggerRuntimeDisplay");
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
    [MenuItem("Tools/Photon Traffic Logger/Start Logging")]
    public static void StartLogging()
    {
        var loggerType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger");
        var logger = loggerType != null ? Object.FindFirstObjectByType(loggerType) : null;
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
            Debug.LogWarning("PhotonTrafficLogger not found. Make sure it's enabled in the settings.");
        }
    }

    [MenuItem("Tools/Photon Traffic Logger/Stop Logging")]
    public static void StopLogging()
    {
        var loggerType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger");
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

    [MenuItem("Tools/Photon Traffic Logger/Clear Logs")]
    public static void ClearLogs()
    {
        var loggerType = System.Type.GetType("PhotonTrafficLogger.PhotonTrafficLogger");
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

    [MenuItem("Tools/Photon Traffic Logger/Attach FusionStats to NetworkRunner")]
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
}