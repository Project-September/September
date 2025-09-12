using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion;
using Result;
using UnityEngine;

namespace September.Common
{
    public class PlayerDatabase : NetworkBehaviour
    {
        [Header("Not Networked")]
        [SerializeField] private ExhibitScoreConfig _config;
        
        [Networked, OnChangedRender(nameof(OnChangedPlayerData)), Capacity(4)]
        public NetworkDictionary<PlayerRef, SessionPlayerData> PlayerDataDic => default;
        public Action<NetworkDictionary<PlayerRef, SessionPlayerData>> ChangedDataAction;
        public static PlayerDatabase Instance;
        
        private readonly Dictionary<PlayerRef, ScoreTracker> _serverTrackers = new();
        private int _currentRoundId = 0;
        
        public override void Spawned()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Runner.Despawn(Object);
            }
        }
        
        public void Server_AddExhibit(PlayerRef actor, ExhibitType type)
        {
            if (!Object.HasStateAuthority)
                return;

            if (!_serverTrackers.TryGetValue(actor, out ScoreTracker tracker))
                _serverTrackers[actor] = tracker = new ScoreTracker(_config);

            tracker.AddInteract(type);
            Debug.Log($"[Server_AddExhibit] {actor} got {type}, count={tracker.GetInteractCount(type)}, total={tracker.CalcTotal()}");

            UpdatePlayerScore(actor, tracker);
        }

        // ホストで集計
        public void Server_AddStun(PlayerRef actor)
        {
            if (!Object.HasStateAuthority)
                return;

            if (!_serverTrackers.TryGetValue(actor, out ScoreTracker tracker))
                _serverTrackers[actor] = tracker = new ScoreTracker(_config);

            tracker.AddStun();

            UpdatePlayerScore(actor, tracker);
        }

        // 合計スコア取得
        public int Server_GetTotal(PlayerRef actor)　=> !_serverTrackers.TryGetValue(actor, out ScoreTracker tracker) ? 0 : tracker.CalcTotal();
        private void UpdatePlayerScore(PlayerRef actor, ScoreTracker tracker)
        {
            if (PlayerDataDic.TryGet(actor, out var d))
            {
                d.Score = tracker.CalcTotal();
                PlayerDataDic.Set(actor, d);
            }
        }
        
        // ホストからクライアントに送信
        public void Server_PushResultToClients()
        {
            if(!Object.HasStateAuthority)
                return;
            
            _currentRoundId++;

            foreach (var kv in PlayerDataDic)
            {
                PlayerRef player = kv.Key;
                SessionPlayerData data =  kv.Value;
                
                // 詳細を取得
                _serverTrackers.TryGetValue(player, out ScoreTracker tracker);

                if (tracker == null)
                {
                    Debug.LogWarning($"[ResultPush] No tracker for {player}.");
                    Rpc_SendPersonalResult( _currentRoundId, "", data.Score);
                    continue;
                }
                
                int calc = tracker.CalcTotal();
                if (calc != data.Score)
                {
                    // エラーだしてもいいかも
                    Debug.LogWarning($"[ResultPush] Mismatch {player}: serverCalc={calc}, netScore={data.Score}. Overwrite netScore.");
                    data.Score = calc;
                    PlayerDataDic.Set(player, data);
                }
                
                // 個人詳細をエンコードして当人にだけ送る
                string payload = EncodeDetailV2(tracker);
                Rpc_SendPersonalResult(_currentRoundId, payload, calc);
            }
        }

        private static string EncodeDetailV2(ScoreTracker tracker)
        {
            StringBuilder sb = new();
            sb.Append("V2|E:");
            bool first = true;
            foreach (KeyValuePair<ExhibitType, int> kv in tracker.ExhibitCounts)
            {
                if (!first)
                    sb.Append(',');
                
                sb.Append(kv.Key).Append('=').Append(kv.Value);
                first = false;
            }
            sb.Append("|S:").Append(tracker.StunCount);
            sb.Append("|G:").Append(tracker.GrapplingHookCount);
            sb.Append("|F:").Append(tracker.FriendExhibitCount);
            return sb.ToString();
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, Channel = RpcChannel.Reliable)]
        private void Rpc_SendPersonalResult( int roundId, string encodedPayload, int pageTotal)
        {
            // クライアント受信：ResultDataInboxへ保存
            if (!ResultDataInbox.I)
            {
                GameObject go = new("[ResultDataInbox]");
                go.AddComponent<ResultDataInbox>();
            }
            ResultDataInbox.I.LoadFromEncoded(roundId, encodedPayload, pageTotal);
        }
        
        public void AddPlayerData(PlayerRef playerRef)
        {
            if (playerRef != Runner.LocalPlayer) return;
            var localNickName = NickNameProvider.GetNickName();
            var nickNameOrder = PlayerDataDic.Count(kv => kv.Value.PureNickName == localNickName);
            Rpc_SetPlayerData(playerRef, new SessionPlayerData(localNickName, nickNameOrder));
        }
        
        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void Rpc_SetPlayerData(PlayerRef playerRef, SessionPlayerData data)
        {
            PlayerDataDic.Set(playerRef, data);
        }
        
        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        public void Rpc_SetCharacter(PlayerRef playerRef, CharacterType characterType)
        {
            if (!PlayerDataDic.TryGet(playerRef, out var playerData)) return;
            playerData.CharacterType = characterType;
            PlayerDataDic.Set(playerRef, playerData);
        }

        void OnChangedPlayerData()
        {
            ChangedDataAction?.Invoke(PlayerDataDic);
        }
    
    }
}

