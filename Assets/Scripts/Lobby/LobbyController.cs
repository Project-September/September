using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using September.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace September.Lobby
{
    public class LobbyController : NetworkBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private PlayerConditionView _playerConditionViewPrefab;
        [SerializeField] private Button _readyButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private TextMeshProUGUI _roomNameText;
        [SerializeField] private Image _fadePanel;
        [SerializeField] private Transform _contentTransform;
        readonly Dictionary<PlayerRef, PlayerConditionView> _lobbyPlayerUIDic = new();
        [Networked, OnChangedRender(nameof(OnChangedIsReady)), Capacity(4), HideInInspector]
        public NetworkDictionary<PlayerRef, NetworkBool> PlayerIsReadyDic => default;
        public override void Spawned()
        {
            _roomNameText.text = Runner.SessionInfo.Name;
            Runner.AddCallbacks(this);
            PlayerDatabase.Instance.AddPlayerData(Runner.LocalPlayer);
            foreach (var pr in Runner.ActivePlayers.Reverse())
            {
                AddContents(pr);
            }
            _readyButton.onClick.AddListener(() => Rpc_ToggleReady(Runner.LocalPlayer));
            _quitButton.onClick.AddListener(() => NetworkManager.Instance.QuitLobby().Forget());
            PlayerDatabase.Instance.ChangedDataAction += ChangeLobbyPlayerUI;
        }
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Runner.RemoveCallbacks(this);
            PlayerDatabase.Instance.ChangedDataAction -= ChangeLobbyPlayerUI;
        }

        private void OnChangedIsReady()
        {
            int isReadyCount = 0;
            foreach (var kv in PlayerIsReadyDic)
            {
                if (kv.Value) isReadyCount++;
                if (!_lobbyPlayerUIDic.TryGetValue(kv.Key, out var value)) return;
                value.IsReadyImage.enabled = kv.Value;
            }
            //  全員準備完了ならゲームを開始する
            if (isReadyCount == PlayerDatabase.Instance.PlayerDataDic.Count)
            {
                RPC_Fade();
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void Rpc_ToggleReady(PlayerRef playerRef)
        {
            PlayerIsReadyDic.Set(playerRef, !PlayerIsReadyDic.Get(playerRef));
        }
        [Rpc]
        private void RPC_Fade()
        {
            RunFadeAndStartGameAsync().Forget();
        }

        private async UniTaskVoid RunFadeAndStartGameAsync()
        {
            if (_fadePanel)
            {
                await NetworkManager.Instance.Fade(_fadePanel);
            }
            NetworkManager.Instance.StartGame().Forget();
        }
        
        void AddContents(PlayerRef playerRef)
        {
            if (_lobbyPlayerUIDic.ContainsKey(playerRef)) return;
            if (Runner.LocalPlayer == playerRef)
            {
                _lobbyPlayerUIDic.Add(playerRef, Instantiate(_playerConditionViewPrefab, _contentTransform));
            }
            else
            {
                _lobbyPlayerUIDic.Add(playerRef, Instantiate(_playerConditionViewPrefab, _contentTransform));
            }
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (HasStateAuthority)
            {
                PlayerIsReadyDic.Add(player, false);
            }
            if (Runner.LocalPlayer == player) return;
            AddContents(player);
        }
        
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Destroy(_lobbyPlayerUIDic[player].gameObject);
            _lobbyPlayerUIDic.Remove(player);
            if (HasStateAuthority)
            {
                PlayerDatabase.Instance.PlayerDataDic.Remove(player);
                PlayerIsReadyDic.Remove(player);
            }
        }
        
        void ChangeLobbyPlayerUI(NetworkDictionary<PlayerRef, SessionPlayerData> dictionary)
        {
            if(dictionary.ContainsKey(Runner.LocalPlayer)) _playerNameText.text = dictionary[Runner.LocalPlayer].DisplayNickName;
            foreach (var kv in dictionary)
            {
                if (!_lobbyPlayerUIDic.TryGetValue(kv.Key, out var value)) return;
                value.PlayerNameText.text = kv.Value.DisplayNickName;
                value.CharacterIconImage.sprite = CharacterDataContainer.Instance.GetCharacterData(kv.Value.CharacterType).CharacterIcon;
            }
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }
        
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
        }
    }
}