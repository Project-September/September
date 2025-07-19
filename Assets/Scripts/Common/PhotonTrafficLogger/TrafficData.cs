using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace PhotonTrafficLogger
{
    [Serializable]
    public class TrafficData
{
    public string callerName;
    public int totalMessageCount;
    public int totalPacketBytes;
    public int longestMessageBytes;
    public DateTime timestamp;
    public string networkObjectName;
    public uint networkObjectId;
    
    public TrafficData(string callerName, int totalMessageCount, int totalPacketBytes, 
        int longestMessageBytes, string networkObjectName = "", uint networkObjectId = 0)
    {
        this.callerName = callerName;
        this.totalMessageCount = totalMessageCount;
        this.totalPacketBytes = totalPacketBytes;
        this.longestMessageBytes = longestMessageBytes;
        this.timestamp = DateTime.Now;
        this.networkObjectName = networkObjectName;
        this.networkObjectId = networkObjectId;
    }
}

[Serializable]
public class TrafficLogEntry
{
    public TrafficData trafficIncoming;
    public TrafficData trafficOutgoing;
    public DateTime timestamp;
    
    public TrafficLogEntry(TrafficData incoming, TrafficData outgoing)
    {
        trafficIncoming = incoming;
        trafficOutgoing = outgoing;
        timestamp = DateTime.Now;
    }
}

[Serializable]
public class TrafficReport
{
    public TrafficStats incomingStats;
    public TrafficStats outgoingStats;
    public DateTime reportStart;
    public DateTime reportEnd;
    public int totalEntries;
    public System.Collections.Generic.Dictionary<string, object> methodStatistics;
    
    public TrafficReport(TrafficStats incoming, TrafficStats outgoing, 
        DateTime start, DateTime end, int totalEntries, 
        System.Collections.Generic.Dictionary<string, object> methodStats = null)
    {
        incomingStats = incoming;
        outgoingStats = outgoing;
        reportStart = start;
        reportEnd = end;
        this.totalEntries = totalEntries;
        methodStatistics = methodStats ?? new System.Collections.Generic.Dictionary<string, object>();
    }
    }

    [Serializable]
    public class TrafficStats
    {
        public int totalMessages;
        public int totalBytes;
        public int maxMessageSize;
        public int minMessageSize;
        public double averageMessageSize;
        public double averageMessagesPerSecond;
        public double averageBytesPerSecond;
        
        public TrafficStats(int totalMessages, int totalBytes, int maxMessageSize, 
            int minMessageSize, double averageMessageSize, double averageMessagesPerSecond, 
            double averageBytesPerSecond)
        {
            this.totalMessages = totalMessages;
            this.totalBytes = totalBytes;
            this.maxMessageSize = maxMessageSize;
            this.minMessageSize = minMessageSize;
            this.averageMessageSize = averageMessageSize;
            this.averageMessagesPerSecond = averageMessagesPerSecond;
            this.averageBytesPerSecond = averageBytesPerSecond;
        }
    }
}