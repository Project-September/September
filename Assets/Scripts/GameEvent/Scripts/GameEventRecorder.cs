using System;
using UnityEngine;
using GameEvent;
using Cysharp.Threading.Tasks;

/// <summary>
/// ゲームのイベントを記録する
/// </summary>
public class GameEventRecorder
{
    const bool UnityEditorTest = false;
    const string ConstTeamID = "Test";
    const string ConstBuildHash = "TGS_Build";

    static GameEventRecorder _instance = new GameEventRecorder();
    private GameEventRecorder() { }

    class EventRecord
    {
        public string GameHash;
        public DateTime GameStartTime;
        public bool IsReview;
        public bool IsGameEnd;
        public bool AlreadySend;
    };

    [Serializable]
    public class CommonEventData
    {
        [SerializeField] string EventName;
        [SerializeField] string TeamID;
        [SerializeField] string BuildHash;
        [SerializeField] string GameHash;
        [SerializeField] int GamePlayTime;
        [SerializeField] GameEventData Payload;

        private CommonEventData() { }
        public CommonEventData(string evtName, GameEventData payload)
        {
            EventName = evtName;
            TeamID = ConstTeamID;
            BuildHash = ConstBuildHash;
            if (_instance._currentGame != null)
            {
                GameHash = _instance._currentGame.GameHash;
                GamePlayTime = (DateTime.Now - _instance._currentGame.GameStartTime).Milliseconds;
            }
            Payload = payload;
        }
    };

    enum SEND_TYPE
    {
        Invalid,
        Start,
        Event
    }

    const string baseuri = "https://jyl5w9zfz3.execute-api.ap-northeast-1.amazonaws.com/release/";
    EventRecord _currentGame = null;
    DateTime _lastSend = DateTime.MinValue;
    SEND_TYPE _lastSendType = SEND_TYPE.Invalid;

#if UNITY_EDITOR
    static bool IsSkipAction => false;
#else
    static bool IsSkipAction => false;
#endif

    /// <summary>
    /// ゲーム開始時に呼び出す
    /// </summary>
    static public void GameStart()
    {
        if (IsSkipAction)
        {
            Debug.Log("GameEventRecorder.GameStart();");
            return;
        }

        if (_instance._currentGame != null)
        {
            Debug.LogWarning("既にプレイ中のゲームがあるようです");
        }

        //ゲームハッシユと現時点の時間を記録する
        _instance._currentGame = new EventRecord();
        _instance._currentGame.GameHash = Guid.NewGuid().ToString();
        _instance._currentGame.GameStartTime = DateTime.Now;

        _instance.SendStart();
    }

    static public void GameEnd(Action reviewEndCallback)
    {
        if (IsSkipAction)
        {
            Debug.Log("GameEventRecorder.GameEnd();");
            return;
        }
        
        _instance._currentGame = null;
    }

    async void SendStart()
    {
        if ((DateTime.Now - _lastSend).Seconds < 1)
        {
            Debug.LogWarning("リクエストが前回から1秒以内に送信されています");
            return;
        }

        var data = new GameEventData();
        data.DataPack("StartTime", _instance._currentGame.GameStartTime);
        data.DataPack("Time", DateTime.Now);
        var packet = new CommonEventData("GameStart", data);
        string json = JsonUtility.ToJson(packet);
        var res = await Network.WebRequest.PostRequest(baseuri + "Event", packet);
        Debug.Log(res);

        _lastSend = DateTime.Now;
        _lastSendType = SEND_TYPE.Start;
    }

    static public async void SendEventData(string situation, GameEventData data)
    {
        if (IsSkipAction)
        {
            Debug.Log("GameEventRecorder.SendEventData();");
            return;
        }

        if ((DateTime.Now - _instance._lastSend).Seconds < 1 && _instance._lastSendType == SEND_TYPE.Event)
        {
            Debug.LogWarning("リクエストが前回から1秒以内に送信されています");
            return;
        }

        var packet = new CommonEventData(situation, data);
        data.DataPack("Time", DateTime.Now);
        await Network.WebRequest.PostRequest(baseuri + "Event", packet);

        _instance._lastSend = DateTime.Now;
        _instance._lastSendType = SEND_TYPE.Event;
    }
}