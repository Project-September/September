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

namespace September.Editor.DebugInGame
{
	[InitializeOnLoad]
	public static class DebugInGameEntryPoint
	{
		static DebugInGameEntryPoint()
		{
			var _lobbyData = DebugInGameDataRepository.TestLobbyData;
			Debug.Log("TestRunner static constructor called. LobbyData loaded: " + (_lobbyData != null));
			if (_lobbyData.IsStartedFromExtensionWindow)
				EditorApplication.playModeStateChanged += StartGame;
		}

		private static void StartGame(PlayModeStateChange mode)
		{
			var lobbyData = DebugInGameDataRepository.TestLobbyData;
			switch (mode)
			{
				case PlayModeStateChange.EnteredPlayMode:
					if (lobbyData == null || lobbyData.IsStartedFromExtensionWindow == false)
						return;

					var testRun = new DebugInGameRunner
					{
						LobbyData = lobbyData
					};
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
		public DebugInGameLobbyData LobbyData;
		private NetworkRunner _networkRunner;
		private float _joinRetrySecond = 15f;
		private float _waitTime = 60;
		private string GameName => LobbyData.LobbyName;
		private int MaxPlayers => LobbyData.PlayerData.Count;
		private List<DebugInGameLobbyData.PlayerSetupData> PlayersData => LobbyData.PlayerData;

		public async UniTask RunAsync()
		{
			var networkManager = NetworkManager.Instance;

			if (SessionState.GetBool("IsMainEditor", false))
				await networkManager.CreateLobby(GameName, MaxPlayers);
			else
				await JoinLobby();
			// NetworkRunnerを強引に取得する
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
				playerDatabase.Rpc_SetCharacter(player.Key, PlayersData[index++].CharacterType);

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
				if (!joinResult.Ok)
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
			_networkRunner = Object.FindFirstObjectByType<NetworkRunner>();
			if (_networkRunner == null)
			{
				Debug.LogWarning("NetworkRunner is null. Cannot add player.");
				return;
			}

			if (!_networkRunner.IsServer) return;
			Debug.Log("AddPlayer" + $" ActivePlayersCount is {_networkRunner.ActivePlayers.Count()}");
			for (var i = 0; i < _networkRunner.ActivePlayers.Count(); i++)
				DebugInGameDataRepository.TestLobbyData.PlayerData[i].IsReady = true;
			DebugInGameDataRepository.EditorUpdate();
		}

		// エディターから開始ボタンが押されるとゲームを開始する
		private async UniTask WaitingForPlayers()
		{
			if (PlayerDatabase.Instance == null) return;
			for (var i = 0; i < _waitTime; i++)
			{
				if (SessionState.GetBool("MoveToGameScene", false))
				{
					Debug.Log("MoveToGameScene flag is set. Moving to game scene.");
					SessionState.SetBool("MoveToGameScene", false);
					break;
				}

				await UniTask.Delay(1000);
			}
		}

		void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
		{
			AddPlayer();
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