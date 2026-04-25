#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using September.Common;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

namespace September.Editor
{
	public class TestRunWindow : EditorWindow
	{
		public static TestRunLobbyData _lobbyData = new();
		private Vector2 _scroll;

		private void OnEnable()
		{
			_lobbyData = TestRunDataRepository.TestLobbyData;
			TestRunDataRepository.OnViewUpdated += Repaint;
		}

		private void OnDisable()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				Debug.Log("Editor is playing or will change playmode. Not saving lobby data.");
			}
			else
			{
				Debug.Log("Editor is playing or will change playmode.");
				_lobbyData.RunFlag = false;
			}

			TestRunDataRepository.SaveLobbyData();
			TestRunDataRepository.OnViewUpdated -= Repaint;
		}

		private void OnGUI()
		{
			_lobbyData.LobbyName = EditorGUILayout.TextField("Lobby Name", _lobbyData.LobbyName);
			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			for (var i = 0; i < _lobbyData.PlayerData.Count; i++)
			{
				EditorGUILayout.BeginHorizontal("box");

				_lobbyData.PlayerData[i].Nickname = EditorGUILayout.TextField(_lobbyData.PlayerData[i].Nickname);
				_lobbyData.PlayerData[i].CharacterType =
					(CharacterType)EditorGUILayout.EnumPopup(_lobbyData.PlayerData[i].CharacterType);

				using (new EditorGUI.DisabledScope(false))
					//if (Application.isPlaying)
				{
					EditorGUILayout.Toggle(_lobbyData.PlayerData[i].IsReady);
				}

				if (GUILayout.Button("削除", GUILayout.Width(60))) _lobbyData.PlayerData.RemoveAt(i);
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndScrollView();

			GUILayout.Space(10);
			if (Application.isPlaying)
				if (GUILayout.Button("MoveToGameScene"))
				{
					Debug.Log("MoveToGameScene button clicked. Setting SessionState flag.");
					SessionState.SetBool("MoveToGameScene", true);
				}

			if (GUILayout.Button("追加")) _lobbyData.PlayerData.Add(new TestRunLobbyData.PlayerSetupData());

			if (GUILayout.Button("Run")) Run();
		}

		[MenuItem("September/Test Run Window")]
		public static void Open()
		{
			GetWindow<TestRunWindow>("Test Run");
		}

		private void Run()
		{
			if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
			{
				Debug.LogWarning("すでに実行中です。");
				return;
			}

			SessionState.SetBool("IsMainEditor", true);
			// ゲーム開始時の設定データをJsonに書き出す
			_lobbyData.RunFlag = true;
			TestRunDataRepository.SaveLobbyData();
			EditorApplication.isPlaying = true;
		}
	}

	public static class TestRunDataRepository
	{
		public static string SavePath => $"{Application.dataPath}/Settings/Editor/TestRunLobbyData.json";
		// ゲーム開始前のロビー設定データを保持するプロパティ
		public static TestRunLobbyData TestLobbyData
		{
			get
			{
				if (_lobbyData == null)
				{
					_lobbyData = LoadLobbyData() ?? new TestRunLobbyData();

					foreach (var player in _lobbyData.PlayerData)
					{
						player.IsReady = false;
					}
				}
				return _lobbyData;
			}
		}
		private static TestRunLobbyData _lobbyData;
		public static event Action OnViewUpdated;

		public static TestRunLobbyData LoadLobbyData()
		{
			if (!File.Exists(SavePath))
			{
				Debug.LogWarning($"JSONファイルが見つかりません: {SavePath}");
				return null;
			}

			var jsonData = File.ReadAllText(SavePath);
			return JsonUtility.FromJson<TestRunLobbyData>(jsonData);
		}

		public static void SaveLobbyData()
		{
			if(_lobbyData==null)
				return;
			var jsonData = JsonUtility.ToJson(_lobbyData);
			var dir = Path.GetDirectoryName(SavePath);
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			File.WriteAllText(SavePath, jsonData);
		}

		public static void EditorUpdate()
		{
			OnViewUpdated?.Invoke();
		}
	}
	
	public class TestRunLobbyData
	{
		public string LobbyName = "TestLobby";
		public List<PlayerSetupData> PlayerData = new();
		public bool RunFlag;

		[Serializable]
		public class PlayerSetupData
		{
			public CharacterType CharacterType = CharacterType.OkabeWright;
			public string Nickname = "TestPlayer";
			public bool IsReady;
		}
	}

	[InitializeOnLoad]
	public static class TestRunner
	{

		static TestRunner()
		{
			var _lobbyData = TestRunDataRepository.TestLobbyData;
			Debug.Log("TestRunner static constructor called. LobbyData loaded: " + (_lobbyData != null));
			if (_lobbyData.RunFlag)
				EditorApplication.playModeStateChanged += StartGame;
		}

		private static void StartGame(PlayModeStateChange mode)
		{
			TestRunLobbyData lobbyData = TestRunDataRepository.TestLobbyData;
			switch (mode)
			{
				case PlayModeStateChange.EnteredPlayMode:
					if (lobbyData == null || lobbyData.RunFlag == false)
						return;

					var testRun = new TestRun
					{
						LobbyData = lobbyData
					};
					testRun.RunAsync().Forget();
					break;

				case PlayModeStateChange.ExitingPlayMode:
					lobbyData.RunFlag = false;
					TestRunDataRepository.SaveLobbyData();
					break;
			}
		}
	}

	public class TestRun : INetworkRunnerCallbacks
	{
		public TestRunLobbyData LobbyData;
		private NetworkRunner _networkRunner;
		public string GameName => LobbyData.LobbyName;
		public int MaxPlayers => LobbyData.PlayerData.Count;
		public List<TestRunLobbyData.PlayerSetupData> PlayersData => LobbyData.PlayerData;
		public float WaitTime = 60;
		public float JoinRetrySecond = 15f;

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
							await UniTask.WaitForSeconds(JoinRetrySecond);
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

			if(!_networkRunner.IsServer) return;
			Debug.Log("AddPlayer" + $" ActivePlayersCount is {_networkRunner.ActivePlayers.Count()}");
			for(var i = 0; i < _networkRunner.ActivePlayers.Count(); i++)
				TestRunDataRepository.TestLobbyData.PlayerData[i].IsReady = true;
			TestRunDataRepository.EditorUpdate();
		}

		// 規定人数に達するorエディターから開始ボタンが押されるとゲームを開始する
		private async UniTask WaitingForPlayers()
		{
			if (PlayerDatabase.Instance == null) return;
			for (var i = 0; i < WaitTime; i++)
			{
				//if (PlayerDatabase.Instance.PlayerDataDic.Count == PlayerData.Count) break;
				if (SessionState.GetBool("MoveToGameScene", false))
				{
					Debug.Log("MoveToGameScene flag is set. Moving to game scene.");
					SessionState.SetBool("MoveToGameScene", false);
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

		void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
		{
			AddPlayer();
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