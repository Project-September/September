using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Fusion;
using Fusion.Statistics;
using Newtonsoft.Json;

public class PhotonTrafficLogger : MonoBehaviour, ITrafficLogger
{
    private PhotonTrafficLoggerSettings _settings;
    private List<TrafficLogEntry> _realtimeLog = new();
    private List<TrafficLogEntry> _accumulatedLog = new();
    private float _logTimer;
    private const float LOGInterval = 1f / 60f; // 60Hz
    
    public bool IsLogging { get; private set; }

    private void Awake()
    {
        _settings = new PhotonTrafficLoggerSettings();
        _settings.LoadFromPrefs();
    }
    
    private void Start()
    {
        if (_settings.Enabled)
        {
            StartLogging();
            AttachFusionStatsToNetworkObjects();
        }
    }
    
    private void Update()
    {
        if (!IsLogging) return;
        
        _logTimer += Time.deltaTime;
        if (_logTimer >= LOGInterval)
        {
            _logTimer = 0f;
            CollectTrafficData();
        }
    }
    
    public void StartLogging()
    {
        if (IsLogging) return;
        
        IsLogging = true;
        _realtimeLog.Clear();
        _accumulatedLog.Clear();
        
        // Create log directory if it doesn't exist
        if (!Directory.Exists(_settings.LogOutputPath))
        {
            Directory.CreateDirectory(_settings.LogOutputPath);
        }
        
        // Clean up old log files
        CleanupOldLogFiles();
        
        Debug.Log("[PhotonTrafficLogger] Started logging");
    }
    
    public void StopLogging()
    {
        if (!IsLogging) return;
        
        IsLogging = false;
        
        // Generate final report
        GenerateReport();
        
        Debug.Log("[PhotonTrafficLogger] Stopped logging");
    }
    
    public void ClearLogs()
    {
        _realtimeLog.Clear();
        _accumulatedLog.Clear();
        Debug.Log("[PhotonTrafficLogger] Cleared logs");
    }
    
    public void LogTraffic(TrafficData incomingData, TrafficData outgoingData, 
        [CallerMemberName] string callerName = "", 
        [CallerFilePath] string callerFilePath = "", 
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (!IsLogging) return;
        
        // Update caller information
        string fullCallerName = $"{Path.GetFileNameWithoutExtension(callerFilePath)}.{callerName}";
        incomingData.callerName = fullCallerName;
        outgoingData.callerName = fullCallerName;
        
        var logEntry = new TrafficLogEntry(incomingData, outgoingData);
        
        // Add to both logs
        _realtimeLog.Add(logEntry);
        _accumulatedLog.Add(logEntry);
        
        // Log to console if enabled
        if (_settings.ConsoleOutput)
        {
            LogToConsole(incomingData, outgoingData, fullCallerName);
        }
        
        // Save realtime log periodically
        if (_realtimeLog.Count % 60 == 0) // Save every 60 entries (1 second at 60Hz)
        {
            SaveRealtimeLog();
        }
    }
    
    private void AttachFusionStatsToNetworkObjects()
    {
        var networkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (var networkObject in networkObjects)
        {
            if (networkObject.GetComponent<FusionStatistics>() == null)
            {
                networkObject.gameObject.AddComponent<FusionStatistics>();
            }
        }
        
        // Enable monitoring for all NetworkObjects
        var fusionStats = FindObjectsByType<FusionStatistics>(FindObjectsSortMode.None);
        foreach (var stats in fusionStats)
        {
            var runner = stats.Runner;
            if (runner != null && runner.TryGetFusionStatistics(out var statisticsManager))
            {
                var networkObject = stats.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    // Enable monitoring for this NetworkObject
                    statisticsManager.ObjectStatisticsManager.MonitorNetworkObjectStatistics(networkObject.Id, true);
                }
            }
        }
        
        Debug.Log($"[PhotonTrafficLogger] Attached FusionStatistics to {networkObjects.Length} NetworkObjects");
    }
    
    private void CollectTrafficData()
    {
        var fusionStats = FindObjectsByType<FusionStatistics>(FindObjectsSortMode.None);
        
        foreach (var stats in fusionStats)
        {
            var networkObject = stats.GetComponent<NetworkObject>();
            if (networkObject == null) continue;
            
            try
            {
                // Get NetworkRunner from FusionStatistics
                var runner = stats.Runner;
                if (runner == null) continue;
                
                // Get statistics manager from runner
                if (runner.TryGetFusionStatistics(out var statisticsManager))
                {
                    // Get traffic data from FusionStatisticsManager
                    var incomingData = GetIncomingTrafficData(statisticsManager, networkObject);
                    var outgoingData = GetOutgoingTrafficData(statisticsManager, networkObject);
                    
                    LogTraffic(incomingData, outgoingData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhotonTrafficLogger] Error collecting traffic data: {e.Message}");
            }
        }
    }
    
    private TrafficData GetIncomingTrafficData(FusionStatisticsManager statisticsManager, NetworkObject networkObject)
    {
        try
        {
            // Get complete snapshot from statistics manager
            var snapshot = statisticsManager.CompleteSnapshot;
            
            // Get network object specific statistics if available
            if (statisticsManager.ObjectStatisticsManager.GetNetworkObjectStatistics(networkObject.Id, out var objectSnapshot))
            {
                return new TrafficData(
                    callerName: "FusionStatisticsManager.GetIncomingTrafficData",
                    totalMessageCount: (int)objectSnapshot.InPackets,
                    totalPacketBytes: (int)objectSnapshot.InBandwidth,
                    longestMessageBytes: 0, // Not directly available from snapshot
                    networkObjectName: networkObject.name,
                    networkObjectId: networkObject.Id.Raw
                );
            }
            else
            {
                // Use global statistics as fallback
                return new TrafficData(
                    callerName: "FusionStatisticsManager.GetIncomingTrafficData",
                    totalMessageCount: (int)snapshot.InPackets,
                    totalPacketBytes: (int)snapshot.InBandwidth,
                    longestMessageBytes: 0, // Not directly available from snapshot
                    networkObjectName: networkObject.name,
                    networkObjectId: networkObject.Id.Raw
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhotonTrafficLogger] Error getting incoming traffic data: {e.Message}");
            return new TrafficData(
                callerName: "FusionStatisticsManager.GetIncomingTrafficData",
                totalMessageCount: 0,
                totalPacketBytes: 0,
                longestMessageBytes: 0,
                networkObjectName: networkObject.name,
                networkObjectId: networkObject.Id.Raw
            );
        }
    }
    
    private TrafficData GetOutgoingTrafficData(FusionStatisticsManager statisticsManager, NetworkObject networkObject)
    {
        try
        {
            // Get complete snapshot from statistics manager
            var snapshot = statisticsManager.CompleteSnapshot;
            
            // Get network object specific statistics if available
            if (statisticsManager.ObjectStatisticsManager.GetNetworkObjectStatistics(networkObject.Id, out var objectSnapshot))
            {
                return new TrafficData(
                    callerName: "FusionStatisticsManager.GetOutgoingTrafficData",
                    totalMessageCount: (int)objectSnapshot.OutPackets,
                    totalPacketBytes: (int)objectSnapshot.OutBandwidth,
                    longestMessageBytes: 0, // Not directly available from snapshot
                    networkObjectName: networkObject.name,
                    networkObjectId: networkObject.Id.Raw
                );
            }
            else
            {
                // Use global statistics as fallback
                return new TrafficData(
                    callerName: "FusionStatisticsManager.GetOutgoingTrafficData",
                    totalMessageCount: (int)snapshot.OutPackets,
                    totalPacketBytes: (int)snapshot.OutBandwidth,
                    longestMessageBytes: 0, // Not directly available from snapshot
                    networkObjectName: networkObject.name,
                    networkObjectId: networkObject.Id.Raw
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhotonTrafficLogger] Error getting outgoing traffic data: {e.Message}");
            return new TrafficData(
                callerName: "FusionStatisticsManager.GetOutgoingTrafficData",
                totalMessageCount: 0,
                totalPacketBytes: 0,
                longestMessageBytes: 0,
                networkObjectName: networkObject.name,
                networkObjectId: networkObject.Id.Raw
            );
        }
    }
    
    private void LogToConsole(TrafficData incomingData, TrafficData outgoingData, string callerName)
    {
        Debug.Log($"[traffic-incoming] 呼び出し元: {callerName} \\n " +
                 $"総メッセージ数: {incomingData.totalMessageCount} \\n " +
                 $"総パケットバイト数: {incomingData.totalPacketBytes} \\n " +
                 $"最大パケットサイズ: {incomingData.longestMessageBytes}");
        
        Debug.Log($"[traffic-outgoing] 呼び出し元: {callerName} \\n " +
                 $"総メッセージ数: {outgoingData.totalMessageCount} \\n " +
                 $"総パケットバイト数: {outgoingData.totalPacketBytes} \\n " +
                 $"最大パケットサイズ: {outgoingData.longestMessageBytes}");
    }
    
    private void SaveRealtimeLog()
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"realtime-{timestamp}.json";
            string filePath = Path.Combine(_settings.LogOutputPath, fileName);
            
            string json = JsonConvert.SerializeObject(_realtimeLog, Formatting.Indented);
            File.WriteAllText(filePath, json);
            
            _realtimeLog.Clear();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhotonTrafficLogger] Failed to save realtime log: {e.Message}");
        }
    }
    
    private void GenerateReport()
    {
        try
        {
            if (_accumulatedLog.Count == 0) return;
            
            var incomingStats = CalculateStats(_accumulatedLog.Select(x => x.trafficIncoming));
            var outgoingStats = CalculateStats(_accumulatedLog.Select(x => x.trafficOutgoing));
            
            var report = new TrafficReport(
                incomingStats,
                outgoingStats,
                _accumulatedLog.First().timestamp,
                _accumulatedLog.Last().timestamp,
                _accumulatedLog.Count
            );
            
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"report-{timestamp}.json";
            string filePath = Path.Combine(_settings.LogOutputPath, fileName);
            
            string json = JsonConvert.SerializeObject(report, Formatting.Indented);
            File.WriteAllText(filePath, json);
            
            Debug.Log($"[PhotonTrafficLogger] Generated report: {fileName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhotonTrafficLogger] Failed to generate report: {e.Message}");
        }
    }
    
    private TrafficStats CalculateStats(IEnumerable<TrafficData> trafficData)
    {
        var dataList = trafficData.ToList();
        if (dataList.Count == 0)
        {
            return new TrafficStats(0, 0, 0, 0, 0, 0, 0);
        }
        
        int totalMessages = dataList.Sum(x => x.totalMessageCount);
        int totalBytes = dataList.Sum(x => x.totalPacketBytes);
        int maxMessageSize = dataList.Max(x => x.longestMessageBytes);
        int minMessageSize = dataList.Min(x => x.longestMessageBytes);
        double averageMessageSize = totalMessages > 0 ? (double)totalBytes / totalMessages : 0;
        
        var timeSpan = dataList.Last().timestamp - dataList.First().timestamp;
        double seconds = timeSpan.TotalSeconds;
        
        double averageMessagesPerSecond = seconds > 0 ? totalMessages / seconds : 0;
        double averageBytesPerSecond = seconds > 0 ? totalBytes / seconds : 0;
        
        return new TrafficStats(
            totalMessages,
            totalBytes,
            maxMessageSize,
            minMessageSize,
            averageMessageSize,
            averageMessagesPerSecond,
            averageBytesPerSecond
        );
    }
    
    private void CleanupOldLogFiles()
    {
        try
        {
            var logFiles = Directory.GetFiles(_settings.LogOutputPath, "*.json")
                .OrderBy(f => File.GetCreationTime(f))
                .ToArray();
            
            int filesToDelete = logFiles.Length - _settings.LogFileLimit;
            if (filesToDelete > 0)
            {
                for (int i = 0; i < filesToDelete; i++)
                {
                    File.Delete(logFiles[i]);
                }
                Debug.Log($"[PhotonTrafficLogger] Deleted {filesToDelete} old log files");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhotonTrafficLogger] Failed to cleanup old log files: {e.Message}");
        }
    }
    
    private void OnDestroy()
    {
        if (IsLogging)
        {
            StopLogging();
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && IsLogging)
        {
            SaveRealtimeLog();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && IsLogging)
        {
            SaveRealtimeLog();
        }
    }
}