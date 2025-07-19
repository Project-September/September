using System.Runtime.CompilerServices;

namespace PhotonTrafficLogger
{
    public interface ITrafficLogger
{
    void LogTraffic(TrafficData incomingData, TrafficData outgoingData, 
        [CallerMemberName] string callerName = "", 
        [CallerFilePath] string callerFilePath = "", 
        [CallerLineNumber] int callerLineNumber = 0);
    
    void StartLogging();
    void StopLogging();
    void ClearLogs();
    bool IsLogging { get; }
    }
}