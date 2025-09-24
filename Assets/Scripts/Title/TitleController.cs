using September.Common;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace September.Title
{
    public class TitleController : MonoBehaviour
    {
        [Header("Create Lobby")]
        
        [SerializeField] TMP_InputField _createLobbyName;
        [SerializeField] Slider _maxPlayers;
        [Header("Join Lobby")]
        [SerializeField] TMP_InputField _joinLobbyName;
        
        public void CreateLobby()
        {
            if (_createLobbyName.text == "" || NickNameProvider.GetNickName() == "") return;
            NetworkManager.Instance.CreateLobby(_createLobbyName.text, (int)_maxPlayers.value);
        }
        
        public void JoinLobby()
        {
            if (_joinLobbyName.text == "" || NickNameProvider.GetNickName() == "") return;
            NetworkManager.Instance.JoinLobby(_joinLobbyName.text);
        }
    }
}