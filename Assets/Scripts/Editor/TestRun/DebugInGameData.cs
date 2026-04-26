using System;
using System.Collections.Generic;
using System.IO;
using September.Common;
using UnityEngine;

namespace September.Editor.DebugInGame
{
	public static class DebugInGameDataRepository
	{
		private static DebugInGameLobbyData _lobbyData;

		public static string SavePath => $"{Application.dataPath}/Settings/Editor/TestRunLobbyData.json";

		// ゲーム開始前のロビー設定データを保持するプロパティ
		public static DebugInGameLobbyData TestLobbyData
		{
			get
			{
				if (_lobbyData == null)
				{
					_lobbyData = LoadLobbyData() ?? new DebugInGameLobbyData();
				}

				return _lobbyData;
			}
		}

		public static event Action OnViewUpdated;

		public static DebugInGameLobbyData LoadLobbyData()
		{
			if (!File.Exists(SavePath))
			{
				Debug.LogWarning($"JSONファイルが見つかりません: {SavePath}");
				return null;
			}

			var jsonData = File.ReadAllText(SavePath);
			return JsonUtility.FromJson<DebugInGameLobbyData>(jsonData);
		}

		public static void SaveLobbyData()
		{
			if (_lobbyData == null)
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
	[Serializable]
	public class DebugInGameLobbyData
	{
		[NonSerialized] public bool RequestMoveToGameScene;
		// 拡張windowからゲームが開始したかどうか
		public bool IsStartedFromExtensionWindow;
		public string LobbyName = "TestLobby";
		public string Nickname = "TestPlayer";
		public List<PlayerSetupData> PlayerData = new();

		[Serializable]
		public class PlayerSetupData
		{
			public CharacterType CharacterType = CharacterType.OkabeWright;
			[NonSerialized]
			public bool IsConnected;
		}
	}
}