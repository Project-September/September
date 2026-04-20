using System.Linq;
using Fusion;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomErrorMessage", menuName = "ScriptableObjects/RoomErrorMessage")]
public class RoomErrorMessage : ScriptableObject
{
    [SerializeField] private ShutdownInfo[] _shutdownInfos;
    [SerializeField] private string _otherErrorMessage;
    [System.Serializable]
    public class ShutdownInfo
    {
        public ShutdownReason Reason;
        public string Message;
    }

    public string GetMessage(ShutdownReason reason)
    {
        var result = _shutdownInfos.FirstOrDefault(x => x.Reason == reason);
        if(result == null)
        {
            return _otherErrorMessage;
        }
        return result.Message;
    }
}
