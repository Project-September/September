using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using September.Common;
using UnityEngine;

public class InGameTestRun : MonoBehaviour
{
	NetworkManager _networkManager;
	string _lobbyName = "TestLobby";
	int _maxPlayers = 3;
	[SerializeField]List<CharacterType> _playerCharacters = new();
	
	
	void Start()
	{
		_networkManager = NetworkManager.Instance;
		CreateLobbyAsync().Forget();
	}
	
	async UniTask CreateLobbyAsync()
	{
		await _networkManager.CreateLobby(_lobbyName, _maxPlayers);
		var playerDatabase = PlayerDatabase.Instance;
		var localPlayerRef = _networkManager.GetLocalPlayerRef();
		playerDatabase.AddPlayerData(localPlayerRef);
		Debug.Log("PlayerID:" + localPlayerRef.PlayerId);
		playerDatabase.Rpc_SetCharacter(localPlayerRef, _playerCharacters[localPlayerRef.PlayerId - 1]);
		_networkManager.StartGame().Forget();
	}
}
