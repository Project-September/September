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
        
        [Header("Ability Bonus Config")]
        [SerializeField] private ExhibitScoreConfig _okabeRideConfig;
        [SerializeField] private ExhibitScoreConfig _haruDestroyConfig;
        [SerializeField] private int _sarutobiBonusScore = 50;
        [SerializeField] private ExhibitScoreConfig _tanihiraBonusScore;
        
        [Networked, OnChangedRender(nameof(OnChangedPlayerData)), Capacity(4)]
        public NetworkDictionary<PlayerRef, SessionPlayerData> PlayerDataDic => default;
        public Action<NetworkDictionary<PlayerRef, SessionPlayerData>> ChangedDataAction;
        public static PlayerDatabase Instance;
        
        private readonly Dictionary<PlayerRef, ScoreTracker> _serverTrackers = new();
        
        public override void Spawned()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                AbilityBonusContainer.Init(_okabeRideConfig, _haruDestroyConfig, _sarutobiBonusScore, _tanihiraBonusScore);
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
            UpdatePlayerScore(actor, tracker);
        }
        
        public void Server_AddGrapplingHook(PlayerRef actor)
        {
            if (!Object.HasStateAuthority)
                return;

            if (!_serverTrackers.TryGetValue(actor, out ScoreTracker tracker))
                _serverTrackers[actor] = tracker = new ScoreTracker(_config);

            tracker.AddGrapplingHook();
            UpdatePlayerScore(actor, tracker);
        }
        
        // ハルクのDestroy処理をここで加算
        public void Server_AddDestroyExhibit(PlayerRef actor, ExhibitType type)
        {
            if(!Object.HasStateAuthority)
                return;

            if (!_serverTrackers.TryGetValue(actor, out ScoreTracker tracker))
            {
                _serverTrackers[actor] = tracker = new ScoreTracker(_config);
            }
            
            tracker.AddDestroyed(type);
            UpdatePlayerScore(actor, tracker);
        }

        // 合計スコア取得
        public int Server_GetTotal(PlayerRef actor)
        {
            if (!_serverTrackers.TryGetValue(actor, out ScoreTracker tracker))
                return 0;

            if (!PlayerDataDic.TryGet(actor, out SessionPlayerData d))
                return tracker.CalcTotal(d);

            return tracker.CalcTotal(d) + AbilityBonusContainer.CalcBonus(d.CharacterType, tracker);
        }
        
        private void UpdatePlayerScore(PlayerRef actor, ScoreTracker tracker)
        {
            if (PlayerDataDic.TryGet(actor, out SessionPlayerData d))
            {
                int baseScore = tracker.CalcTotal(d);
                int bonus = AbilityBonusContainer.CalcBonus(d.CharacterType, tracker);
                
                d.Score = baseScore + bonus;
                PlayerDataDic.Set(actor, d);
            }
        }
        
        // ホストからクライアントに送信
        public void Server_PushResultToClients()
        {
            if(!Object.HasStateAuthority)
                return;

            foreach (var kv in PlayerDataDic)
            {
                PlayerRef player = kv.Key;
                SessionPlayerData data =  kv.Value;
                _serverTrackers.TryGetValue(player, out ScoreTracker tracker);

                if (tracker == null)
                {
                    Debug.LogWarning($"[ResultPush] No tracker for {player}.");
                    Rpc_SendPersonalResult( player,"", data.Score);
                    continue;
                }
                
                int calc = tracker.CalcTotal(data) + AbilityBonusContainer.CalcBonus(data.CharacterType, tracker);

                if (calc != data.Score)
                {
                    // エラーだしてもいいかも
                    Debug.LogWarning($"[ResultPush] Mismatch {player}: serverCalc={calc}, netScore={data.Score}. Overwrite netScore.");
                    data.Score = calc;
                    PlayerDataDic.Set(player, data);
                }
                
                string payload = EncodeDetailV2(tracker);
                Rpc_SendPersonalResult(player, payload, calc);
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

            if (tracker.DestroyedExhibitCounts.Count > 0)
            {
                sb.Append("|D:");
                first = true;
                foreach (KeyValuePair<ExhibitType, int> kv in tracker.DestroyedExhibitCounts)
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }
                    sb.Append(kv.Key).Append('=').Append(kv.Value);
                    first = false;
                }
            }
            sb.Append("|G:").Append(tracker.GrapplingHookCount);
            sb.Append("|F:").Append(tracker.FriendExhibitCount);
            return sb.ToString();
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_SendPersonalResult(PlayerRef target, string encodedPayload, int pageTotal)
        {
            if (Runner.LocalPlayer != target) 
                return;
            
            if (!ResultDataInbox.I)
            {
                GameObject go = new("[ResultDataInbox]");
                go.AddComponent<ResultDataInbox>();
            }
            ResultDataInbox.I.LoadFromEncoded(encodedPayload, pageTotal);
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

