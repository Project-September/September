using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using Fusion.Statistics;

namespace PhotonTrafficLogger
{
    public class PhotonTrafficLoggerRuntimeDisplay : MonoBehaviour
{
    [Header("Runtime Display Settings")]
    [SerializeField] private bool showRuntimeDisplay = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F9;
    [SerializeField] private int maxDisplayEntries = 10;
    [SerializeField] private float displayUpdateInterval = 0.5f;
    
    private PhotonTrafficLogger _trafficLogger;
    private readonly List<TrafficLogEntry> _recentEntries = new();
    private Rect _windowRect = new(10, 10, 400, 300);
    private Vector2 _scrollPosition = Vector2.zero;
    private bool _isDisplayActive;
    private float _lastUpdateTime;
    private GUIStyle _headerStyle;
    private GUIStyle _contentStyle;
    private GUIStyle _toggleStyle;
    private bool _stylesInitialized;
    
    private void Start()
    {
        _trafficLogger = GetComponent<PhotonTrafficLogger>();
        if (_trafficLogger == null)
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
        
        _isDisplayActive = showRuntimeDisplay;
        _lastUpdateTime = Time.time;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _isDisplayActive = !_isDisplayActive;
        }
        
        if (_isDisplayActive && Time.time - _lastUpdateTime >= displayUpdateInterval)
        {
            UpdateDisplayData();
            _lastUpdateTime = Time.time;
        }
    }
    
    private void OnGUI()
    {
        if (!_isDisplayActive || _trafficLogger == null || !_trafficLogger.IsLogging)
            return;
        
        InitializeStyles();
        
        _windowRect = GUILayout.Window(0, _windowRect, DrawTrafficWindow, "Photon Traffic Logger");
    }
    
    private void InitializeStyles()
    {
        if (_stylesInitialized) return;
        
        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            normal = { textColor = Color.white }
        };
        
        _contentStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            normal = { textColor = Color.cyan },
            wordWrap = true
        };
        
        _toggleStyle = new GUIStyle(GUI.skin.toggle)
        {
            normal = { textColor = Color.white }
        };
        
        _stylesInitialized = true;
    }
    
    private void DrawTrafficWindow(int windowID)
    {
        GUILayout.BeginVertical();
        
        // Header
        GUILayout.Label("Photon Network Traffic Monitor", _headerStyle);
        GUILayout.Label($"Press {toggleKey} to toggle display", _contentStyle);
        
        GUILayout.Space(5);
        
        // Controls
        GUILayout.BeginHorizontal();
        showRuntimeDisplay = GUILayout.Toggle(showRuntimeDisplay, "Auto Show", _toggleStyle);
        
        if (GUILayout.Button("Clear"))
        {
            _recentEntries.Clear();
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
        GUILayout.Label("Recent Traffic Data:", _headerStyle);
        
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
        
        if (_recentEntries.Count == 0)
        {
            GUILayout.Label("No traffic data available", _contentStyle);
        }
        else
        {
            for (var i = _recentEntries.Count - 1; i >= 0; i--)
            {
                DrawTrafficEntry(_recentEntries[i]);
            }
        }
        
        GUILayout.EndScrollView();
        
        GUILayout.EndVertical();
        
        GUI.DragWindow();
    }
    
    private void DrawStatistics()
    {
        if (_recentEntries.Count == 0) return;
        
        var totalIncoming = _recentEntries.Sum(e => e.trafficIncoming.totalPacketBytes);
        var totalOutgoing = _recentEntries.Sum(e => e.trafficOutgoing.totalPacketBytes);
        var totalMessages = _recentEntries.Sum(e => e.trafficIncoming.totalMessageCount + e.trafficOutgoing.totalMessageCount);
        
        GUILayout.Label("Current Session Statistics:", _headerStyle);
        GUILayout.Label($"Total Incoming: {totalIncoming} bytes", _contentStyle);
        GUILayout.Label($"Total Outgoing: {totalOutgoing} bytes", _contentStyle);
        GUILayout.Label($"Total Messages: {totalMessages}", _contentStyle);
        GUILayout.Label($"Active NetworkObjects: {GetActiveNetworkObjectCount()}", _contentStyle);
    }
    
    private void DrawTrafficEntry(TrafficLogEntry entry)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        
        GUILayout.Label($"Time: {entry.timestamp:HH:mm:ss}", _contentStyle);
        
        if (entry.trafficIncoming != null)
        {
            GUILayout.Label($"↓ IN: {entry.trafficIncoming.totalPacketBytes}B | " +
                           $"Msgs: {entry.trafficIncoming.totalMessageCount} | " +
                           $"Max: {entry.trafficIncoming.longestMessageBytes}B", 
                           _contentStyle);
            
            if (!string.IsNullOrEmpty(entry.trafficIncoming.networkObjectName))
            {
                GUILayout.Label($"Object: {entry.trafficIncoming.networkObjectName}", _contentStyle);
            }
        }
        
        if (entry.trafficOutgoing != null)
        {
            GUILayout.Label($"↑ OUT: {entry.trafficOutgoing.totalPacketBytes}B | " +
                           $"Msgs: {entry.trafficOutgoing.totalMessageCount} | " +
                           $"Max: {entry.trafficOutgoing.longestMessageBytes}B", 
                           _contentStyle);
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
                                objectSnapshot.InPackets,
                                (int)objectSnapshot.InBandwidth,
                                0, // Not available from snapshot
                                networkObject.name,
                                networkObject.Id.Raw
                            );
                            
                            outgoingData = new TrafficData(
                                "RuntimeDisplay.UpdateDisplayData",
                                objectSnapshot.OutPackets,
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
                                snapshot.InPackets,
                                (int)snapshot.InBandwidth,
                                0, // Not available from snapshot
                                networkObject.name,
                                networkObject.Id.Raw
                            );
                            
                            outgoingData = new TrafficData(
                                "RuntimeDisplay.UpdateDisplayData",
                                snapshot.OutPackets,
                                (int)snapshot.OutBandwidth,
                                0, // Not available from snapshot
                                networkObject.name,
                                networkObject.Id.Raw
                            );
                        }
                        
                        var entry = new TrafficLogEntry(incomingData, outgoingData);
                        _recentEntries.Add(entry);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[PhotonTrafficLoggerRuntimeDisplay] Error getting traffic data: {e.Message}");
                    }
                }
            }
        }
        
        // Keep only the most recent entries
        if (_recentEntries.Count > maxDisplayEntries)
        {
            _recentEntries.RemoveRange(0, _recentEntries.Count - maxDisplayEntries);
        }
    }
    
    private int GetActiveNetworkObjectCount()
    {
        return FindObjectsByType<NetworkObject>(FindObjectsSortMode.None).Length;
    }
    
    private void ExportCurrentData()
    {
        if (_recentEntries.Count == 0)
        {
            Debug.Log("[PhotonTrafficLoggerRuntimeDisplay] No data to export");
            return;
        }
        
        try
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(_recentEntries, Newtonsoft.Json.Formatting.Indented);
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
}