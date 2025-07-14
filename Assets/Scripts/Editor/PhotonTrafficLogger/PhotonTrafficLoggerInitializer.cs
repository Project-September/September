using UnityEngine;
using UnityEditor;
using Fusion;
using Fusion.Statistics;

[InitializeOnLoad]
public class PhotonTrafficLoggerInitializer
{
    static PhotonTrafficLoggerInitializer()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            InitializeTrafficLogger();
        }
    }
    
    private static void InitializeTrafficLogger()
    {
        var settings = new PhotonTrafficLoggerSettings();
        settings.LoadFromPrefs();
        
        if (!settings.Enabled)
            return;
        
        // Find or create the traffic logger
        var existingLogger = Object.FindObjectOfType<PhotonTrafficLogger>();
        if (existingLogger != null)
            return;
        
        // Create a new GameObject for the traffic logger
        var loggerObject = new GameObject("PhotonTrafficLogger");
        var logger = loggerObject.AddComponent<PhotonTrafficLogger>();
        
        // Add runtime display component for development builds
        if (Debug.isDebugBuild)
        {
            loggerObject.AddComponent<PhotonTrafficLoggerRuntimeDisplay>();
        }
        
        // Make it persist across scenes
        Object.DontDestroyOnLoad(loggerObject);
        
        Debug.Log("[PhotonTrafficLoggerInitializer] Initialized traffic logger");
    }
}

public class PhotonTrafficLoggerMenuItem
{
    [MenuItem("Tools/Photon Traffic Logger/Start Logging")]
    public static void StartLogging()
    {
        var logger = Object.FindObjectOfType<PhotonTrafficLogger>();
        if (logger != null)
        {
            logger.StartLogging();
            Debug.Log("Started Photon traffic logging");
        }
        else
        {
            Debug.LogWarning("PhotonTrafficLogger not found. Make sure it's enabled in the settings.");
        }
    }
    
    [MenuItem("Tools/Photon Traffic Logger/Stop Logging")]
    public static void StopLogging()
    {
        var logger = Object.FindObjectOfType<PhotonTrafficLogger>();
        if (logger != null)
        {
            logger.StopLogging();
            Debug.Log("Stopped Photon traffic logging");
        }
        else
        {
            Debug.LogWarning("PhotonTrafficLogger not found.");
        }
    }
    
    [MenuItem("Tools/Photon Traffic Logger/Clear Logs")]
    public static void ClearLogs()
    {
        var logger = Object.FindObjectOfType<PhotonTrafficLogger>();
        if (logger != null)
        {
            logger.ClearLogs();
            Debug.Log("Cleared Photon traffic logs");
        }
        else
        {
            Debug.LogWarning("PhotonTrafficLogger not found.");
        }
    }
    
    [MenuItem("Tools/Photon Traffic Logger/Attach FusionStats to NetworkObjects")]
    public static void AttachFusionStatsToNetworkObjects()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("This operation can only be performed in Play mode.");
            return;
        }
        
        var networkObjects = Object.FindObjectsOfType<NetworkObject>();
        int attachedCount = 0;
        
        foreach (var networkObject in networkObjects)
        {
            if (networkObject.GetComponent<FusionStatistics>() == null)
            {
                networkObject.gameObject.AddComponent<FusionStatistics>();
                attachedCount++;
            }
        }
        
        Debug.Log($"Attached FusionStats to {attachedCount} NetworkObjects (Total: {networkObjects.Length})");
    }
}