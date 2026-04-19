using September.Common;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

namespace September.Title
{
    public class TitleController : MonoBehaviour
    {
        [Header("Create Lobby")]

        [SerializeField] TMP_InputField _createLobbyName;
        [SerializeField] Slider _maxPlayers;
        [Header("Join Lobby")]
        [SerializeField] TMP_InputField _joinLobbyName;
        [Header("Error Message")]
        [SerializeField] TextMeshProUGUI _createMessageText;
        [SerializeField] TextMeshProUGUI _joinMessageText;
        [SerializeField] RoomErrorMessage _roomErrorMessage;
        public void Start()
        {
            _createMessageText?.gameObject.SetActive(false);
            _joinMessageText?.gameObject.SetActive(false);
        }

        public async void CreateLobby()
        {
            if (_createLobbyName.text == "" || NickNameProvider.GetNickName() == "") return;
            var result = await NetworkManager.Instance.CreateLobby(_createLobbyName.text, (int)_maxPlayers.value);
            ChangeErrorMessage(result, _createMessageText);
        }

        public async void JoinLobby()
        {
            if (_joinLobbyName.text == "" || NickNameProvider.GetNickName() == "") return;
            var result = await NetworkManager.Instance.JoinLobby(_joinLobbyName.text);
            ChangeErrorMessage(result, _joinMessageText);
        }
        /// <summary>
        /// StartGameの結果に応じてエラーメッセージを変更する
        /// </summary>
        /// <param name="result">StartGameの結果</param>
        /// <param name="text">エラーメッセージを表示するText</param>
        private void ChangeErrorMessage(StartGameResult result, TextMeshProUGUI text)
        {
            if (result == null || text == null || _roomErrorMessage == null) return;

            if (result.Ok)
            {
                text.gameObject.SetActive(false);
                return;
            }

            text.gameObject.SetActive(true);
            text.text = _roomErrorMessage.GetMessage(result.ShutdownReason);
        }
    }
}