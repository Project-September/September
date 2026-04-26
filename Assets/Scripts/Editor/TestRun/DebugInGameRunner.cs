#region

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using September.Common;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

namespace September.Editor.DebugInGame
{
	[InitializeOnLoad]
	public static class DebugInGameEntryPoint
	{
		static DebugInGameEntryPoint()
		{
			var _lobbyData = DebugInGameDataRepository.TestLobbyData;
			if (_lobbyData.IsStartedFromExtensionWindow)
			{
				EditorApplication.playModeStateChanged += StartGame;
			}
		}

		private static void StartGame(PlayModeStateChange mode)
		{
			var lobbyData = DebugInGameDataRepository.TestLobbyData;
			switch (mode)
			{
				case PlayModeStateChange.EnteredPlayMode:
					if (lobbyData == null || lobbyData.IsStartedFromExtensionWindow == false)
						return;
					var testRun = new DebugInGameRunner();
					testRun.RunAsync().Forget();
					break;

				case PlayModeStateChange.ExitingPlayMode:
					lobbyData.IsStartedFromExtensionWindow = false;
					DebugInGameDataRepository.SaveLobbyData();
					break;
			}
		}
	}

	public class DebugInGameRunner : INetworkRunnerCallbacks
	{
		private readonly float _joinRetrySecond = 15f;
		private NetworkRunner _networkRunner;
		private DebugInGameLobbyData _lobbyData;
		private string GameName => _lobbyData.LobbyName;
		private int MaxPlayers => _lobbyData.PlayerData.Count;
		private List<DebugInGameLobbyData.PlayerSetupData> PlayersData => _lobbyData.PlayerData;

		void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
		{
			AddPlayer();
		}

		public async UniTask RunAsync()
		{
			var networkManager = NetworkManager.Instance;
			_lobbyData = DebugInGameDataRepository.TestLobbyData;
			NickNameProvider.SetNickName(_lobbyData.Nickname);

			// これがメインエディターであればLobbyを作成する。
			if (SessionState.GetBool("IsMainEditor", false))
				await networkManager.CreateLobby(GameName, MaxPlayers);
			else　// メインエディターでなければLobbyに入れる
				await JoinLobby();
			// NetworkRunnerをFindで取得する
			_networkRunner = Object.FindFirstObjectByType<NetworkRunner>();
			_networkRunner.AddCallbacks(this);
			AddPlayer();

			await UniTask.WaitUntil(() => PlayerDatabase.Instance != null);
			var playerDatabase = PlayerDatabase.Instance;
			var localPlayerRef = networkManager.GetLocalPlayerRef();
			playerDatabase.AddPlayerData(localPlayerRef);

			if (!_networkRunner.IsServer) return;
			// 指定人数に達するか、設定された時間が終了するまで待つ
			await WaitingForPlayers();

			// 設定されたキャラクターを人数分、順番に割り振る
			var index = 0;
			foreach (var player in playerDatabase.PlayerDataDic)
			{
				if(index < PlayersData.Count)
					playerDatabase.Rpc_SetCharacter(player.Key, PlayersData[index++].CharacterType);
			}

			networkManager.StartGame().Forget();
		}

		private async UniTask JoinLobby()
		{
			for (var i = 0; i < 10; i++)
			{
				Debug.Log("Try to join lobby: " + GameName);
				var joinResult = await NetworkManager.Instance.JoinLobby(GameName);
				if (joinResult.Ok)
					return;
				switch (joinResult.ShutdownReason)
				{
					case ShutdownReason.ConnectionTimeout:
					case ShutdownReason.ConnectionRefused:
					case ShutdownReason.GameNotFound:
						await UniTask.WaitForSeconds(_joinRetrySecond);
						continue;
					default:
						Debug.LogError($"Failed to join lobby: {joinResult.ShutdownReason}");
						return;
				}
			}
		}

		private void AddPlayer()
		{
			if (!_networkRunner.IsServer) return;
			if (_networkRunner == null)
			{
				Debug.LogWarning("NetworkRunner is null. Cannot add player.");
				return;
			}

			// アクティブプレイヤーの数だけ順番にConnectしたことにする。
			for (var i = 0; i < _networkRunner.ActivePlayers.Count() &&
			     i < _lobbyData.PlayerData.Count; i++)
			{
				_lobbyData.PlayerData[i].IsConnected = true;
			}

			DebugInGameDataRepository.EditorUpdate();
		}

		// エディターから開始ボタンが押されるまで待機する。
		private async UniTask WaitingForPlayers()
		{
			if (PlayerDatabase.Instance == null) return;
			while (true)
			{
				if (_lobbyData.RequestMoveToGameScene)
				{
					Debug.Log("MoveToGameScene flag is set. Moving to game scene.");
					break;
				}

				await UniTask.Delay(1000);
			}
		}

		#region INetworkRunnerCallbacks

		void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
		{
		}

		void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
		{
		}

		void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
		{
		}

		void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
		{
		}

		void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
		{
		}

		void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner,
			NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
		{
		}

		void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress,
			NetConnectFailedReason reason)
		{
		}

		void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
		{
		}

		void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key,
			ArraySegment<byte> data)
		{
		}

		void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key,
			float progress)
		{
		}

		void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
		{
		}

		void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
		{
		}

		void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
		{
		}

		void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
		{
		}

		void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner,
			Dictionary<string, object> data)
		{
		}

		void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
		{
		}

		void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
		{
		}

		void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
		{
		}

		#endregion
	}
}