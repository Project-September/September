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
        public TextMeshProUGUI PlayerNameText => _playerNameText;
        public Image CharacterIconImage => _characterIconImage;
        public Image IsReadyImage => _isReadyImage;
    }
}