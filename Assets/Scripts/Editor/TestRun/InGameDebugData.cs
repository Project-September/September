#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using September.Common;
using UnityEditor;
using UnityEngine;

namespace September.Editor.InGameDebug
{
	public static class InGameDebugDataRepository
	{
		private static InGameDebugLobbyData _lobbyData;

		public static string SavePath => $"{Application.dataPath}/Settings/Editor/Local/TestRunLobbyData.json";

		// ゲーム開始前のロビー設定データを保持するプロパティ
		public static InGameDebugLobbyData TestLobbyData
		{
			get
			{
				if (_lobbyData == null)
				{
					_lobbyData = LoadLobbyData() ?? new InGameDebugLobbyData();
				}

				return _lobbyData;
			}
		}

		public static event Action OnViewUpdated;

		public static InGameDebugLobbyData LoadLobbyData()
		{
			if (!File.Exists(SavePath))
			{
				Debug.LogWarning($"JSONファイルが見つかりません: {SavePath}");
				return null;
			}

			var jsonData = File.ReadAllText(SavePath);
			return JsonUtility.FromJson<InGameDebugLobbyData>(jsonData);
		}

		public static void SaveLobbyData()
		{
			if (_lobbyData == null)
				return;
			var jsonData = JsonUtility.ToJson(_lobbyData);
			var dir = Path.GetDirectoryName(SavePath);
			if (!string.IsNullOrEmpty(dir))
			{
				Directory.CreateDirectory(dir);
			}
			File.WriteAllText(SavePath, jsonData);
			AssetDatabase.Refresh();
		}
		
		public static void EditorUpdate()
		{
			OnViewUpdated?.Invoke();
		}
	}

	[Serializable]
	public class InGameDebugLobbyData
	{
		// 拡張windowからゲームが開始したかどうか
		public bool IsStartedFromExtensionWindow;
		public string LobbyName = "TestLobby";
		public string Nickname = "TestPlayer";
		public List<PlayerSetupData> PlayerData = new();
		[NonSerialized] public bool RequestMoveToGameScene;

		[Serializable]
		public class PlayerSetupData
		{
			public CharacterType CharacterType = CharacterType.OkabeWright;

			[NonSerialized] public bool IsConnected;
		}
	}
}
#endif