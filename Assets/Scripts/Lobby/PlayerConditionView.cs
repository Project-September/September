using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace September.Lobby
{
    public class PlayerConditionView : MonoBehaviour
    {
        [SerializeField, DisplayName("プレイヤー名表示用")] TextMeshProUGUI _playerNameText;
        [SerializeField, DisplayName("キャラアイコン表示用")] Image _characterIconImage;
        [SerializeField, DisplayName("準備完了アイコン")] Image _isReadyImage;
        [SerializeField, DisplayName("Bot用キャラ変更ボタン")] Button _characterChangeButton;
        [SerializeField, DisplayName("Bot用削除ボタン")] Button _botRemoveButton;
        
        public TextMeshProUGUI PlayerNameText => _playerNameText;
        public Image CharacterIconImage => _characterIconImage;
        public Image IsReadyImage => _isReadyImage;
        public Button CharacterChangeButton => _characterChangeButton;
        public Button BotRemoveButton => _botRemoveButton;
    }
}