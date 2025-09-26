using GameEvent;
using UnityEngine;

/// <summary>
/// イベントテスト送信
/// </summary>
public class EventTest : MonoBehaviour
{
    [SerializeField] GameEventData _data;

    public void SendSampleEvent()
    {
        //
        var data = new GameEventData();
        data.DataPack("EnemyType", 1);
        data.DataPack("Stage", 1);
        GameEventRecorder.SendEventData("EnemyKill", data);
    }

    public void SendSerializeData()
    {
        //
        GameEventRecorder.SendEventData("Sample", _data);
    }
}