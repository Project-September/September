using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using Fusion.Statistics;

public class PhotonTrafficLoggerRuntimeDisplay : MonoBehaviour
{
    [Header("Runtime Display Settings")]
    [SerializeField] private bool showRuntimeDisplay = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F9;
    [SerializeField] private int maxDisplayEntries = 10;
    [SerializeField] private float displayUpdateInterval = 0.5f;
    
    private PhotonTrafficLogger trafficLogger;
    private List<TrafficLogEntry> recentEntries = new List<TrafficLogEntry>();
    private Rect windowRect = new Rect(10, 10, 400, 300);
    private Vector2 scrollPosition = Vector2.zero;
    private bool isDisplayActive = false;
    private float lastUpdateTime;
    private GUIStyle headerStyle;
    private GUIStyle contentStyle;
    private GUIStyle toggleStyle;
    private bool stylesInitialized = false;
    
    private void Start()
    {
        trafficLogger = GetComponent<PhotonTrafficLogger>();
        if (trafficLogger == null)
        {
            Debug.LogError("[PhotonTrafficLoggerRuntimeDisplay] PhotonTrafficLogger component not found!");
            enabled = false;
            return;
        }
        
        // Only enable in development builds
        if (!Debug.isDebugBuild)
        {
            enabled = false;
            return;
        }
        
        isDisplayActive = showRuntimeDisplay;
        lastUpdateTime = Time.time;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isDisplayActive = !isDisplayActive;
        }
        
        if (isDisplayActive && Time.time - lastUpdateTime >= displayUpdateInterval)
        {
            UpdateDisplayData();
            lastUpdateTime = Time.time;
        }
    }
    
    private void OnGUI()
    {
        if (!isDisplayActive || trafficLogger == null || !trafficLogger.IsLogging)
            return;
        
        InitializeStyles();
        
        windowRect = GUILayout.Window(0, windowRect, DrawTrafficWindow, "Photon Traffic Logger");
    }
    
    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            normal = { textColor = Color.white }
        };
        
        contentStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            normal = { textColor = Color.cyan },
            wordWrap = true
        };
        
        toggleStyle = new GUIStyle(GUI.skin.toggle)
        {
            normal = { textColor = Color.white }
        };
        
        stylesInitialized = true;
    }
    
    private void DrawTrafficWindow(int windowID)
    {
        GUILayout.BeginVertical();
        
        // Header
        GUILayout.Label("Photon Network Traffic Monitor", headerStyle);
        GUILayout.Label($"Press {toggleKey} to toggle display", contentStyle);
        
        GUILayout.Space(5);
        
        // Controls
        GUILayout.BeginHorizontal();
        showRuntimeDisplay = GUILayout.Toggle(showRuntimeDisplay, "Auto Show", toggleStyle);
        
        if (GUILayout.Button("Clear"))
        {
            recentEntries.Clear();
        }
        
        if (GUILayout.Button("Export"))
        {
            ExportCurrentData();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // Statistics
        DrawStatistics();
        
        GUILayout.Space(5);
        
        // Recent entries
        GUILayout.Label("Recent Traffic Data:", headerStyle);
        
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
        
        if (recentEntries.Count == 0)
        {
            GUILayout.Label("No traffic data available", contentStyle);
        }
        else
        {
            for (int i = recentEntries.Count - 1; i >= 0; i--)
            {
                DrawTrafficEntry(recentEntries[i]);
            }
        }
        
        GUILayout.EndScrollView();
        
        GUILayout.EndVertical();
        
        GUI.DragWindow();
    }
    
    private void DrawStatistics()
    {
        if (recentEntries.Count == 0) return;
        
        var totalIncoming = recentEntries.Sum(e => e.trafficIncoming.totalPacketBytes);
        var totalOutgoing = recentEntries.Sum(e => e.trafficOutgoing.totalPacketBytes);
        var totalMessages = recentEntries.Sum(e => e.trafficIncoming.totalMessageCount + e.trafficOutgoing.totalMessageCount);
        
        GUILayout.Label("Current Session Statistics:", headerStyle);
        GUILayout.Label($"Total Incoming: {totalIncoming} bytes", contentStyle);
        GUILayout.Label($"Total Outgoing: {totalOutgoing} bytes", contentStyle);
        GUILayout.Label($"Total Messages: {totalMessages}", contentStyle);
        GUILayout.Label($"Active NetworkObjects: {GetActiveNetworkObjectCount()}", contentStyle);
    }
    
    private void DrawTrafficEntry(TrafficLogEntry entry)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        
        GUILayout.Label($"Time: {entry.timestamp:HH:mm:ss}", contentStyle);
        
        if (entry.trafficIncoming != null)
        {
            GUILayout.Label($"↓ IN: {entry.trafficIncoming.totalPacketBytes}B | " +
                           $"Msgs: {entry.trafficIncoming.totalMessageCount} | " +
                           $"Max: {entry.trafficIncoming.longestMessageBytes}B", 
                           contentStyle);
            
            if (!string.IsNullOrEmpty(entry.trafficIncoming.networkObjectName))
            {
                GUILayout.Label($"Object: {entry.trafficIncoming.networkObjectName}", contentStyle);
            }
        }
        
        if (entry.trafficOutgoing != null)
        {
            GUILayout.Label($"↑ OUT: {entry.trafficOutgoing.totalPacketBytes}B | " +
                           $"Msgs: {entry.trafficOutgoing.totalMessageCount} | " +
                           $"Max: {entry.trafficOutgoing.longestMessageBytes}B", 
                           contentStyle);
        }
        
        GUILayout.EndVertical();
    }
    
    private void UpdateDisplayData()
    {
        // Get real traffic data from FusionStatistics
        var fusionStats = FindObjectsByType<FusionStatistics>(FindObjectsSortMode.None);
        
        foreach (var stats in fusionStats)
        {
            var networkObject = stats.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                var runner = stats.Runner;
                if (runner != null && runner.TryGetFusionStatistics(out var statisticsManager))
                {
                    try
                    {
                        // Get actual traffic data from FusionStatisticsManager
                        var snapshot = statisticsManager.CompleteSnapshot;
                        
                        TrafficData incomingData, outgoingData;
                        
                        // Try to get network object specific statistics
                        if (statisticsManager.ObjectStatisticsManager.GetNetworkObjectStatistics(networkObject.Id, out var objectSnapshot))
                        {
                            incomingData = new TrafficData(
                                "RuntimeDisplay.UpdateDisplayData",
                                (int)objectSnapshot.InPackets,
                                (int)objectSnapshot.InBandwidth,
                                0, // Not available from snapshot
                                networkObject.name,
                                networkObject.Id.Raw
                            );
                            
                            outgoingData = new TrafficData(
                                "RuntimeDisplay.UpdateDisplayData",
                                (int)objectSnapshot.OutPackets,
                                (int)objectSnapshot.OutBandwidth,
                                0, // Not available from snapshot
                                networkObject.name,
                                networkObject.Id.Raw
                            );
                        }
                        else
                        {
                            // Use global statistics as fallback
                            incomingData = new TrafficData(
                                "RuntimeDisplay.UpdateDisplayData",
                                (int)snapshot.InPackets,
                                (int)snapshot.InBandwidth,
                                0, // Not available from snapshot
                                networkObject.name,
                                networkObject.Id.Raw
                            );
                            
                            outgoingData = new TrafficData(
                                "RuntimeDisplay.UpdateDisplayData",
                                (int)snapshot.OutPackets,
                                (int)snapshot.OutBandwidth,
                                0, // Not available from snapshot
                                networkObject.name,
                                networkObject.Id.Raw
                            );
                        }
                        
                        var entry = new TrafficLogEntry(incomingData, outgoingData);
                        recentEntries.Add(entry);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[PhotonTrafficLoggerRuntimeDisplay] Error getting traffic data: {e.Message}");
                    }
                }
            }
        }
        
        // Keep only the most recent entries
        if (recentEntries.Count > maxDisplayEntries)
        {
            recentEntries.RemoveRange(0, recentEntries.Count - maxDisplayEntries);
        }
    }
    
    private int GetActiveNetworkObjectCount()
    {
        return FindObjectsOfType<NetworkObject>().Length;
    }
    
    private void ExportCurrentData()
    {
        if (recentEntries.Count == 0)
        {
            Debug.Log("[PhotonTrafficLoggerRuntimeDisplay] No data to export");
            return;
        }
        
        try
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(recentEntries, Newtonsoft.Json.Formatting.Indented);
            string timestamp = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"runtime-export-{timestamp}.json";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            
            System.IO.File.WriteAllText(filePath, json);
            Debug.Log($"[PhotonTrafficLoggerRuntimeDisplay] Exported data to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PhotonTrafficLoggerRuntimeDisplay] Failed to export data: {e.Message}");
        }
    }
}