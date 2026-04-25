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

namespace September.Editor.DebugInGame
{
	public class DebugLobbyWindow : EditorWindow
	{
		public static DebugInGameLobbyData _lobbyData = new();
		private Vector2 _scroll;

		private void OnEnable()
		{
			_lobbyData = DebugInGameDataRepository.TestLobbyData;
			DebugInGameDataRepository.OnViewUpdated += Repaint;
		}

		private void OnDisable()
		{
			if (!EditorApplication.isPlayingOrWillChangePlaymode)
			{
				_lobbyData.IsStartedFromExtensionWindow = false;
			}

			DebugInGameDataRepository.SaveLobbyData();
			DebugInGameDataRepository.OnViewUpdated -= Repaint;
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
				{
					if (Application.isPlaying) EditorGUILayout.Toggle(_lobbyData.PlayerData[i].IsReady);
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

			if (GUILayout.Button("追加")) _lobbyData.PlayerData.Add(new DebugInGameLobbyData.PlayerSetupData());

			if (GUILayout.Button("Run")) Run();
		}

		[MenuItem("September/Test Run Window")]
		public static void Open()
		{
			GetWindow<DebugLobbyWindow>("Test Run");
		}

		private void Run()
		{
			if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
			{
				Debug.LogWarning("すでに実行中です。");
				return;
			}

			SessionState.SetBool("IsMainEditor", true);
			// ゲーム開始時の設定データをJsonに書き出す(DomainReload対策)
			_lobbyData.IsStartedFromExtensionWindow = true;
			DebugInGameDataRepository.SaveLobbyData();
			// この後はDomainReloadが入るため、データのリセットが入る
			EditorApplication.isPlaying = true;
		}
	}
}