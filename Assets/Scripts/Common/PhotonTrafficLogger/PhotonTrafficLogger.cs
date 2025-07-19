using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;
using Fusion;
using Fusion.Statistics;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;

namespace PhotonTrafficLogger
{
    public class PhotonTrafficLogger : MonoBehaviour, ITrafficLogger
{
    private PhotonTrafficLoggerSettings _settings;
    private List<TrafficLogEntry> _realtimeLog = new();
    private List<TrafficLogEntry> _accumulatedLog = new();
    private float _logTimer;
    private const float LOGInterval = 1f / 60f; // 60Hz
    
    // Packet size tracking
    private int _maxIncomingPacketSize;
    private int _maxOutgoingPacketSize;
    private readonly Dictionary<NetworkRunner, int> _previousIncomingBytes = new();
    private readonly Dictionary<NetworkRunner, int> _previousOutgoingBytes = new();
    private readonly Dictionary<NetworkRunner, int> _previousIncomingPackets = new();
    private readonly Dictionary<NetworkRunner, int> _previousOutgoingPackets = new();

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

        // Reset packet size tracking
        _maxIncomingPacketSize = 0;
        _maxOutgoingPacketSize = 0;
        _previousIncomingBytes.Clear();
        _previousOutgoingBytes.Clear();
        _previousIncomingPackets.Clear();
        _previousOutgoingPackets.Clear();

        // Create log directory if it doesn't exist
        if (!Directory.Exists(_settings.LogOutputPath))
        {
            Directory.CreateDirectory(_settings.LogOutputPath);
        }

        // Clean up old log files
        CleanupOldLogFiles();

        Debug.Log("[PhotonTrafficLogger] Started logging with packet size tracking");
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

        // Use stack trace based detection for external calls
        var detailedCallerInfo = GetDetailedCallerInfo();
        
        var fullCallerName = !string.IsNullOrEmpty(detailedCallerInfo) 
            ? detailedCallerInfo 
            : $"External.{Path.GetFileNameWithoutExtension(callerFilePath)}.{callerName}";
            
        incomingData.callerName = fullCallerName;
        outgoingData.callerName = fullCallerName;

        var logEntry = new TrafficLogEntry(incomingData, outgoingData);

        // Add to both logs
        _realtimeLog.Add(logEntry);
        _accumulatedLog.Add(logEntry);

        // Debug log for data collection
        Debug.Log(
            $"[PhotonTrafficLogger] Logged traffic data - In: {incomingData.totalMessageCount} msgs, {incomingData.totalPacketBytes} bytes | Out: {outgoingData.totalMessageCount} msgs, {outgoingData.totalPacketBytes} bytes");

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

    private string GetContextualCallerInfo(NetworkRunner runner, TrafficData incomingData, TrafficData outgoingData)
    {
        try
        {
            // Create meaningful caller information based on NetworkRunner state and data patterns
            var contextInfo = new List<string>();
            
            // Add NetworkRunner context
            if (runner.IsServer)
            {
                contextInfo.Add("Server");
            }
            else if (runner.IsClient)
            {
                contextInfo.Add("Client");
            }
            
            // Analyze traffic patterns to infer likely activity
            bool hasIncomingTraffic = incomingData.totalMessageCount > 0 || incomingData.totalPacketBytes > 0;
            bool hasOutgoingTraffic = outgoingData.totalMessageCount > 0 || outgoingData.totalPacketBytes > 0;
            
            if (hasIncomingTraffic && hasOutgoingTraffic)
            {
                contextInfo.Add("Bidirectional");
            }
            else if (hasIncomingTraffic)
            {
                contextInfo.Add("Receiving");
            }
            else if (hasOutgoingTraffic)
            {
                contextInfo.Add("Sending");
            }
            
            // Try to infer activity type based on packet characteristics
            var activityType = InferActivityType(incomingData, outgoingData);
            if (!string.IsNullOrEmpty(activityType))
            {
                contextInfo.Add(activityType);
            }
            
            // Check for active network objects
            var activeObjects = GetActiveNetworkObjectInfo(runner);
            if (!string.IsNullOrEmpty(activeObjects))
            {
                contextInfo.Add(activeObjects);
            }
            
            var result = contextInfo.Count > 0 
                ? $"NetworkActivity.{string.Join(".", contextInfo)}"
                : "NetworkActivity.Unknown";
                
            Debug.Log($"[PhotonTrafficLogger] Generated contextual caller: {result}");
            return result;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PhotonTrafficLogger] Error generating contextual caller info: {e.Message}");
            return "NetworkActivity.Error";
        }
    }
    
    private string InferActivityType(TrafficData incomingData, TrafficData outgoingData)
    {
        // Infer activity type based on data patterns
        int totalMessages = incomingData.totalMessageCount + outgoingData.totalMessageCount;
        int totalBytes = incomingData.totalPacketBytes + outgoingData.totalPacketBytes;
        
        if (totalMessages == 0 && totalBytes == 0)
        {
            return "Idle";
        }
        
        // Small, frequent packets - likely continuous updates
        if (totalMessages > 5 && totalBytes < 1000)
        {
            return "ContinuousUpdate";
        }
        
        // Large packets - likely significant state changes
        if (totalBytes > 1000)
        {
            return "StateSync";
        }
        
        // Medium traffic - likely gameplay events
        if (totalMessages > 0 && totalBytes > 100)
        {
            return "GameplayEvent";
        }
        
        return "LowActivity";
    }
    
    private string GetActiveNetworkObjectInfo(NetworkRunner runner)
    {
        try
        {
            // Try to get information about active network objects
            var networkObjects = FindObjectsByType<NetworkBehaviour>(FindObjectsSortMode.None);
            var activeCount = networkObjects.Count(obj => obj != null && obj.Runner == runner);
            
            if (activeCount > 0)
            {
                // Check for specific types of network objects
                var players = networkObjects.Count(obj => obj != null && obj.Runner == runner && 
                                                  obj.GetType().Name.Contains("Player"));
                
                if (players > 0)
                {
                    return $"Players({players})";
                }
                
                return $"Objects({activeCount})";
            }
            
            return null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PhotonTrafficLogger] Error getting network object info: {e.Message}");
            return null;
        }
    }
    
    private void LogTrafficDirect(TrafficData incomingData, TrafficData outgoingData)
    {
        if (!IsLogging) return;

        var logEntry = new TrafficLogEntry(incomingData, outgoingData);

        // Add to both logs
        _realtimeLog.Add(logEntry);
        _accumulatedLog.Add(logEntry);

        // Debug log for data collection
        Debug.Log(
            $"[PhotonTrafficLogger] Logged traffic data - In: {incomingData.totalMessageCount} msgs, {incomingData.totalPacketBytes} bytes | Out: {outgoingData.totalMessageCount} msgs, {outgoingData.totalPacketBytes} bytes");

        // Log to console if enabled
        if (_settings.ConsoleOutput)
        {
            LogToConsole(incomingData, outgoingData, incomingData.callerName);
        }

        // Save realtime log periodically
        if (_realtimeLog.Count % 60 == 0) // Save every 60 entries (1 second at 60Hz)
        {
            SaveRealtimeLog();
        }
    }

    private string GetDetailedCallerInfo()
    {
        try
        {
            var stackTrace = new StackTrace(true);
            var frames = stackTrace.GetFrames();
            
            if (frames == null) 
            {
                Debug.LogWarning("[PhotonTrafficLogger] Stack trace frames are null");
                return null;
            }

            string firstUserMethod = null;
            
            // Skip our own methods and look for meaningful caller
            for (int i = 0; i < frames.Length; i++)
            {
                var frame = frames[i];
                var method = frame.GetMethod();
                
                if (method == null) continue;
                
                var declaringType = method.DeclaringType;
                if (declaringType == null) continue;
                
                var typeName = declaringType.Name;
                var methodName = method.Name;
                var namespaceName = declaringType.Namespace ?? "";
                
                // Skip our own PhotonTrafficLogger methods
                if (typeName == "PhotonTrafficLogger") continue;
                
                // Skip system/unity internal methods
                if (IsSystemMethod(namespaceName, typeName, methodName)) continue;
                
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();
                
                var shortFileName = !string.IsNullOrEmpty(fileName) 
                    ? Path.GetFileNameWithoutExtension(fileName) 
                    : typeName;
                
                var methodInfo = lineNumber > 0 
                    ? $"{shortFileName}.{methodName}:{lineNumber}"
                    : $"{shortFileName}.{methodName}";
                
                // Look for Fusion/Network related methods that might be sending packets
                if (IsFusionNetworkMethod(namespaceName, typeName, methodName))
                {
                    return methodInfo;
                }
                
                // Store first user code method as fallback
                if (firstUserMethod == null && IsUserCode(namespaceName, typeName))
                {
                    firstUserMethod = methodInfo;
                }
            }
            // Return first user method if no network method was found
            return firstUserMethod;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PhotonTrafficLogger] Error getting detailed caller info: {e.Message}");
            return null;
        }
    }
    
    private bool IsSystemMethod(string namespaceName, string typeName, string methodName)
    {
        // Skip Unity and system methods
        return namespaceName.StartsWith("UnityEngine") ||
               namespaceName.StartsWith("Unity.") ||
               namespaceName.StartsWith("System") ||
               namespaceName.StartsWith("Microsoft") ||
               typeName.StartsWith("Runtime") ||
               methodName.StartsWith("Internal") ||
               methodName == "Update" ||
               methodName == "FixedUpdate" ||
               methodName == "LateUpdate";
    }
    
    private bool IsFusionNetworkMethod(string namespaceName, string typeName, string methodName)
    {
        // Look for Fusion networking methods that likely send packets
        if (namespaceName.StartsWith("Fusion") ||
            typeName.Contains("Network") ||
            typeName.Contains("RPC") ||
            typeName.Contains("Spawn"))
        {
            return true;
        }
        
        // Check method names that indicate network operations
        if (methodName.Contains("RPC") ||
            methodName.Contains("Send") ||
            methodName.Contains("Sync") ||
            methodName.Contains("Network") ||
            methodName.Contains("Spawn") ||
            methodName.Contains("Despawn") ||
            methodName.Contains("Transfer") ||
            methodName.Contains("Communicate"))
        {
            return true;
        }
        
        // Check for game-specific networking patterns
        if (typeName.Contains("Player") || 
            typeName.Contains("Manager") ||
            typeName.Contains("Controller"))
        {
            // Look for methods that might trigger network activity
            if (methodName.Contains("Move") ||
                methodName.Contains("Attack") ||
                methodName.Contains("Action") ||
                methodName.Contains("Input") ||
                methodName.Contains("Update") ||
                methodName.Contains("Execute") ||
                methodName.Contains("Ability"))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private bool IsUserCode(string namespaceName, string typeName)
    {
        // Identify user code (not system or Unity)
        bool isUserCode = !namespaceName.StartsWith("UnityEngine") &&
                         !namespaceName.StartsWith("Unity.") &&
                         !namespaceName.StartsWith("System") &&
                         !namespaceName.StartsWith("Microsoft") &&
                         !namespaceName.StartsWith("Fusion.Statistics") &&
                         !typeName.StartsWith("Runtime");
        
        return isUserCode;
    }

    private void AttachFusionStatsToNetworkObjects()
    {
        // FusionStatistics should be attached to NetworkRunner, not NetworkObjects
        var networkRunners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        foreach (var networkRunner in networkRunners)
        {
            if (networkRunner.GetComponent<FusionStatistics>() == null)
            {
                networkRunner.gameObject.AddComponent<FusionStatistics>();
                Debug.Log($"[PhotonTrafficLogger] Added FusionStatistics to NetworkRunner: {networkRunner.name}");
            }
        }

        Debug.Log($"[PhotonTrafficLogger] Attached FusionStatistics to {networkRunners.Length} NetworkRunners");
    }

    private void CollectTrafficData()
    {
        var networkRunners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);

        if (networkRunners.Length == 0)
        {
            Debug.LogWarning("[PhotonTrafficLogger] No NetworkRunners found in scene");
            return;
        }

        foreach (var runner in networkRunners)
        {
            try
            {
                // NetworkRunnerが有効で、接続されている場合のみデータを収集
                if (runner == null)
                {
                    Debug.LogWarning("[PhotonTrafficLogger] NetworkRunner is null");
                    continue;
                }

                if (!runner.IsRunning)
                {
                    Debug.LogWarning($"[PhotonTrafficLogger] NetworkRunner {runner.name} is not running");
                    continue;
                }

                Debug.Log(
                    $"[PhotonTrafficLogger] NetworkRunner {runner.name} - IsRunning: {runner.IsRunning}, IsServer: {runner.IsServer}");

                // Get statistics manager from runner
                if (runner.TryGetFusionStatistics(out var statisticsManager))
                {
                    Debug.Log($"[PhotonTrafficLogger] Collecting data from NetworkRunner: {runner.name}");

                    // Get traffic data from FusionStatisticsManager
                    var incomingData = GetIncomingTrafficData(statisticsManager, runner);
                    var outgoingData = GetOutgoingTrafficData(statisticsManager, runner);

                    // Override caller information to provide more meaningful context
                    var contextualCaller = GetContextualCallerInfo(runner, incomingData, outgoingData);
                    incomingData.callerName = contextualCaller;
                    outgoingData.callerName = contextualCaller;

                    LogTrafficDirect(incomingData, outgoingData);
                }
                else
                {
                    Debug.LogWarning(
                        $"[PhotonTrafficLogger] FusionStatistics not found on NetworkRunner: {runner.name}");
                    // フォールバック: NetworkRunnerから直接統計を取得
                    TryCollectFallbackData(runner);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PhotonTrafficLogger] Error collecting traffic data: {e.Message}");
            }
        }
    }

    private void TryCollectFallbackData(NetworkRunner runner)
    {
        try
        {
            // NetworkRunnerから直接統計データを取得（簡易版）
            Debug.Log($"[PhotonTrafficLogger] Using fallback data collection from NetworkRunner: {runner.name}");

            var incomingData = new TrafficData(
                callerName: "NetworkActivity.Fallback.NoStatistics",
                totalMessageCount: 0,
                totalPacketBytes: 0,
                longestMessageBytes: 0,
                networkObjectName: runner.name,
                networkObjectId: 0
            );

            var outgoingData = new TrafficData(
                callerName: "NetworkActivity.Fallback.NoStatistics",
                totalMessageCount: 0,
                totalPacketBytes: 0,
                longestMessageBytes: 0,
                networkObjectName: runner.name,
                networkObjectId: 0
            );

            LogTrafficDirect(incomingData, outgoingData);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhotonTrafficLogger] Error in fallback data collection: {e.Message}");
        }
    }

    private TrafficData GetIncomingTrafficData(FusionStatisticsManager statisticsManager, NetworkRunner runner)
    {
        try
        {
            // Try to get snapshot directly from statistics manager
            var snapshot = statisticsManager.CompleteSnapshot;

            if (snapshot == null)
            {
                Debug.LogWarning("[PhotonTrafficLogger] CompleteSnapshot is null");
                return CreateEmptyTrafficData("FusionStatisticsManager.GetIncomingTrafficData", runner.name);
            }

            Debug.Log(
                $"[PhotonTrafficLogger] Incoming data - InPackets: {snapshot.InPackets}, InBandwidth: {snapshot.InBandwidth}");

            // Try to get detailed statistics for max packet size
            int maxPacketSize = GetMaxPacketSize(statisticsManager, true); // true for incoming

            return new TrafficData(
                callerName: "FusionStatisticsManager.GetIncomingTrafficData",
                totalMessageCount: snapshot.InPackets,
                totalPacketBytes: (int)snapshot.InBandwidth,
                longestMessageBytes: maxPacketSize,
                networkObjectName: runner.name,
                networkObjectId: 0
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhotonTrafficLogger] Error getting incoming traffic data: {e.Message}");
            return CreateEmptyTrafficData("FusionStatisticsManager.GetIncomingTrafficData", runner.name);
        }
    }

    private TrafficData GetOutgoingTrafficData(FusionStatisticsManager statisticsManager, NetworkRunner runner)
    {
        try
        {
            // Try to get snapshot directly from statistics manager
            var snapshot = statisticsManager.CompleteSnapshot;

            if (snapshot == null)
            {
                Debug.LogWarning("[PhotonTrafficLogger] CompleteSnapshot is null");
                return CreateEmptyTrafficData("FusionStatisticsManager.GetOutgoingTrafficData", runner.name);
            }

            Debug.Log(
                $"[PhotonTrafficLogger] Outgoing data - OutPackets: {snapshot.OutPackets}, OutBandwidth: {snapshot.OutBandwidth}");

            // Try to get detailed statistics for max packet size
            int maxPacketSize = GetMaxPacketSize(statisticsManager, false); // false for outgoing

            return new TrafficData(
                callerName: "FusionStatisticsManager.GetOutgoingTrafficData",
                totalMessageCount: snapshot.OutPackets,
                totalPacketBytes: (int)snapshot.OutBandwidth,
                longestMessageBytes: maxPacketSize,
                networkObjectName: runner.name,
                networkObjectId: 0
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhotonTrafficLogger] Error getting outgoing traffic data: {e.Message}");
            return CreateEmptyTrafficData("FusionStatisticsManager.GetOutgoingTrafficData", runner.name);
        }
    }

    private int GetMaxPacketSize(FusionStatisticsManager statisticsManager, bool isIncoming)
    {
        try
        {
            var snapshot = statisticsManager.CompleteSnapshot;
            if (snapshot == null) return isIncoming ? _maxIncomingPacketSize : _maxOutgoingPacketSize;

            // Get NetworkRunner from statistics manager
            var networkRunner = GetNetworkRunnerFromStatisticsManager(statisticsManager);
            if (networkRunner == null) 
            {
                Debug.LogWarning("[PhotonTrafficLogger] Could not find NetworkRunner for statistics manager");
                return isIncoming ? _maxIncomingPacketSize : _maxOutgoingPacketSize;
            }

            // Method 1: Calculate max packet size from incremental data
            int incrementalMaxSize = CalculateIncrementalMaxPacketSize(networkRunner, snapshot, isIncoming);
            if (incrementalMaxSize > 0)
            {
                if (isIncoming)
                {
                    _maxIncomingPacketSize = Math.Max(_maxIncomingPacketSize, incrementalMaxSize);
                    Debug.Log($"[PhotonTrafficLogger] Updated max incoming packet size: {_maxIncomingPacketSize}");
                    return _maxIncomingPacketSize;
                }
                else
                {
                    _maxOutgoingPacketSize = Math.Max(_maxOutgoingPacketSize, incrementalMaxSize);
                    Debug.Log($"[PhotonTrafficLogger] Updated max outgoing packet size: {_maxOutgoingPacketSize}");
                    return _maxOutgoingPacketSize;
                }
            }

            // Method 2: Try to get from detailed network statistics
            int detailedMaxSize = TryGetMaxPacketFromDetailedStats(statisticsManager, isIncoming);
            if (detailedMaxSize > 0)
            {
                Debug.Log($"[PhotonTrafficLogger] Found max packet size from detailed stats: {detailedMaxSize}");
                return detailedMaxSize;
            }

            // Method 3: Calculate estimated max packet size from available data
            int estimatedMaxSize = EstimateMaxPacketSize(snapshot, isIncoming);
            if (estimatedMaxSize > 0)
            {
                Debug.Log($"[PhotonTrafficLogger] Estimated max packet size: {estimatedMaxSize}");
                return estimatedMaxSize;
            }

            // Return the tracked maximum if no other method worked
            return isIncoming ? _maxIncomingPacketSize : _maxOutgoingPacketSize;
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhotonTrafficLogger] Error getting max packet size: {e.Message}");
            return isIncoming ? _maxIncomingPacketSize : _maxOutgoingPacketSize;
        }
    }

    private NetworkRunner GetNetworkRunnerFromStatisticsManager(FusionStatisticsManager statisticsManager)
    {
        try
        {
            // Try to get the NetworkRunner associated with this statistics manager
            var networkRunners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
            foreach (var runner in networkRunners)
            {
                if (runner.TryGetFusionStatistics(out var runnerStatsManager) && runnerStatsManager == statisticsManager)
                {
                    return runner;
                }
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PhotonTrafficLogger] Error finding NetworkRunner: {e.Message}");
            return null;
        }
    }

    private int CalculateIncrementalMaxPacketSize(NetworkRunner runner, FusionStatisticsSnapshot snapshot, bool isIncoming)
    {
        try
        {
            if (isIncoming)
            {
                // Get current values
                int currentBytes = (int)snapshot.InBandwidth;
                int currentPackets = snapshot.InPackets;

                // Get previous values
                if (_previousIncomingBytes.TryGetValue(runner, out int prevBytes) &&
                    _previousIncomingPackets.TryGetValue(runner, out int prevPackets))
                {
                    int byteDelta = currentBytes - prevBytes;
                    int packetDelta = currentPackets - prevPackets;

                    if (packetDelta > 0 && byteDelta > 0)
                    {
                        // Calculate average packet size for this interval
                        int currentIntervalPacketSize = byteDelta / packetDelta;
                        
                        Debug.Log($"[PhotonTrafficLogger] Incoming packet size this interval: {currentIntervalPacketSize} bytes " +
                                 $"(delta: {byteDelta} bytes / {packetDelta} packets)");
                        
                        // Update previous values
                        _previousIncomingBytes[runner] = currentBytes;
                        _previousIncomingPackets[runner] = currentPackets;
                        
                        return currentIntervalPacketSize;
                    }
                }

                // Store current values for next iteration
                _previousIncomingBytes[runner] = currentBytes;
                _previousIncomingPackets[runner] = currentPackets;
            }
            else
            {
                // Similar logic for outgoing
                int currentBytes = (int)snapshot.OutBandwidth;
                int currentPackets = snapshot.OutPackets;

                if (_previousOutgoingBytes.TryGetValue(runner, out int prevBytes) &&
                    _previousOutgoingPackets.TryGetValue(runner, out int prevPackets))
                {
                    int byteDelta = currentBytes - prevBytes;
                    int packetDelta = currentPackets - prevPackets;

                    if (packetDelta > 0 && byteDelta > 0)
                    {
                        int currentIntervalPacketSize = byteDelta / packetDelta;
                        
                        Debug.Log($"[PhotonTrafficLogger] Outgoing packet size this interval: {currentIntervalPacketSize} bytes " +
                                 $"(delta: {byteDelta} bytes / {packetDelta} packets)");
                        
                        _previousOutgoingBytes[runner] = currentBytes;
                        _previousOutgoingPackets[runner] = currentPackets;
                        
                        return currentIntervalPacketSize;
                    }
                }

                _previousOutgoingBytes[runner] = currentBytes;
                _previousOutgoingPackets[runner] = currentPackets;
            }

            return 0;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PhotonTrafficLogger] Error calculating incremental packet size: {e.Message}");
            return 0;
        }
    }

    private int TryGetMaxPacketFromDetailedStats(FusionStatisticsManager statisticsManager, bool isIncoming)
    {
        try
        {
            Debug.Log($"[PhotonTrafficLogger] Trying to get detailed packet statistics (isIncoming: {isIncoming})");
            
            // Debug: Print available methods in FusionStatisticsManager
            var type = statisticsManager.GetType();
            Debug.Log($"[PhotonTrafficLogger] FusionStatisticsManager type: {type.FullName}");
            
            // Try various approaches based on what might be available in Fusion Statistics
            
            // Approach 1: Check for packet statistics properties/methods
            try
            {
                // Try to access packet-level statistics if available
                var snapshot = statisticsManager.CompleteSnapshot;
                if (snapshot != null)
                {
                    // Check available properties in the snapshot
                    var snapshotType = snapshot.GetType();
                    Debug.Log($"[PhotonTrafficLogger] FusionStatisticsSnapshot properties available:");
                    foreach (var prop in snapshotType.GetProperties())
                    {
                        Debug.Log($"  - {prop.Name}: {prop.PropertyType}");
                    }
                    
                    // Try to find packet size related properties
                    var maxPacketSizeProperty = snapshotType.GetProperty("MaxPacketSize");
                    if (maxPacketSizeProperty != null)
                    {
                        var maxSize = (int)maxPacketSizeProperty.GetValue(snapshot);
                        Debug.Log($"[PhotonTrafficLogger] Found MaxPacketSize property: {maxSize}");
                        return maxSize;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhotonTrafficLogger] Could not access snapshot properties: {e.Message}");
            }

            // Approach 2: Try to access historical packet data
            try
            {
                // Some statistics managers provide historical data
                var statsType = statisticsManager.GetType();
                var historyMethod = statsType.GetMethod("GetPacketHistory") ?? 
                                   statsType.GetMethod("GetTrafficHistory") ??
                                   statsType.GetMethod("GetDetailedStatistics");
                
                if (historyMethod != null)
                {
                    Debug.Log($"[PhotonTrafficLogger] Found history method: {historyMethod.Name}");
                    var historyData = historyMethod.Invoke(statisticsManager, null);
                    if (historyData != null)
                    {
                        Debug.Log($"[PhotonTrafficLogger] Retrieved history data: {historyData.GetType()}");
                        // Process history data to find max packet size
                        return ProcessHistoryDataForMaxPacketSize(historyData, isIncoming);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PhotonTrafficLogger] Could not access packet history: {e.Message}");
            }

            return 0;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PhotonTrafficLogger] Could not get detailed packet stats: {e.Message}");
            return 0;
        }
    }
    
    private int ProcessHistoryDataForMaxPacketSize(object historyData, bool isIncoming)
    {
        try
        {
            // Process any historical packet data to find maximum packet size
            if (historyData == null) return 0;
            
            var dataType = historyData.GetType();
            Debug.Log($"[PhotonTrafficLogger] Processing {(isIncoming ? "incoming" : "outgoing")} history data type: {dataType.FullName}");
            
            // Try to access array or collection of packet data
            if (dataType.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(dataType))
            {
                Debug.Log($"[PhotonTrafficLogger] History data is enumerable, searching for {(isIncoming ? "incoming" : "outgoing")} packet sizes");
                // Process enumerable data
            }
            
            return 0;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PhotonTrafficLogger] Error processing history data: {e.Message}");
            return 0;
        }
    }

    private int EstimateMaxPacketSize(FusionStatisticsSnapshot snapshot, bool isIncoming)
    {
        try
        {
            // Estimate max packet size based on available data
            if (isIncoming)
            {
                if (snapshot.InPackets > 0)
                {
                    // Simple estimation: total bandwidth / packet count gives average
                    // We'll multiply by 2 as a rough estimate for max packet size
                    int averageSize = (int)(snapshot.InBandwidth / snapshot.InPackets);
                    int estimatedMax = averageSize * 2; // Rough estimation
                    
                    // Cap at reasonable maximum (MTU is typically 1500 bytes)
                    return Math.Min(estimatedMax, 1500);
                }
            }
            else
            {
                if (snapshot.OutPackets > 0)
                {
                    int averageSize = (int)(snapshot.OutBandwidth / snapshot.OutPackets);
                    int estimatedMax = averageSize * 2; // Rough estimation
                    
                    return Math.Min(estimatedMax, 1500);
                }
            }

            return 0;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PhotonTrafficLogger] Could not estimate max packet size: {e.Message}");
            return 0;
        }
    }

    private TrafficData CreateEmptyTrafficData(string callerName, string networkObjectName)
    {
        return new TrafficData(
            callerName: callerName,
            totalMessageCount: 0,
            totalPacketBytes: 0,
            longestMessageBytes: 0,
            networkObjectName: networkObjectName,
            networkObjectId: 0
        );
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
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"realtime-{timestamp}.json";
            var filePath = Path.Combine(_settings.LogOutputPath, fileName);

            var json = JsonConvert.SerializeObject(_realtimeLog, Formatting.Indented);
            File.WriteAllText(filePath, json);

            Debug.Log($"[PhotonTrafficLogger] Saved realtime log: {fileName} with {_realtimeLog.Count} entries");

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
            if (_accumulatedLog.Count == 0)
            {
                Debug.LogWarning("[PhotonTrafficLogger] No accumulated log data to generate report");
                return;
            }

            var incomingStats = CalculateStats(_accumulatedLog.Select(x => x.trafficIncoming));
            var outgoingStats = CalculateStats(_accumulatedLog.Select(x => x.trafficOutgoing));

            var report = new TrafficReport(
                incomingStats,
                outgoingStats,
                _accumulatedLog.First().timestamp,
                _accumulatedLog.Last().timestamp,
                _accumulatedLog.Count
            );

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = $"report-{timestamp}.json";
            var filePath = Path.Combine(_settings.LogOutputPath, fileName);

            var json = JsonConvert.SerializeObject(report, Formatting.Indented);
            File.WriteAllText(filePath, json);

            Debug.Log($"[PhotonTrafficLogger] Generated report: {fileName} with {_accumulatedLog.Count} entries");
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

        var totalMessages = dataList.Sum(x => x.totalMessageCount);
        var totalBytes = dataList.Sum(x => x.totalPacketBytes);
        var maxMessageSize = dataList.Max(x => x.longestMessageBytes);
        var minMessageSize = dataList.Min(x => x.longestMessageBytes);
        var averageMessageSize = totalMessages > 0 ? (double)totalBytes / totalMessages : 0;

        var timeSpan = dataList.Last().timestamp - dataList.First().timestamp;
        var seconds = timeSpan.TotalSeconds;

        var averageMessagesPerSecond = seconds > 0 ? totalMessages / seconds : 0;
        var averageBytesPerSecond = seconds > 0 ? totalBytes / seconds : 0;

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
                .OrderBy(File.GetCreationTime)
                .ToArray();

            var filesToDelete = logFiles.Length - _settings.LogFileLimit;
            if (filesToDelete > 0)
            {
                for (var i = 0; i < filesToDelete; i++)
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
}