using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NewResult.UI
{
    public interface IPlayerDetailsView
    {
        void Set(PlayerDetailsModel model);
    }
    
    public class PlayerDetailsView : MonoBehaviour, IPlayerDetailsView
    {
        [SerializeField] private Image _characterIcon;
        [SerializeField] private TextMeshProUGUI _playerName;
        [SerializeField] private TextMeshProUGUI _playerScore;
        [SerializeField] private TextMeshProUGUI _playerDamageDealt;
        [SerializeField] private TextMeshProUGUI _playerDamageReceived;
        [SerializeField] private TextMeshProUGUI _playerOgreCount;
        [SerializeField] private TextMeshProUGUI _playerExhibitsInteractCount;

        public void Set(PlayerDetailsModel model)
        {
            _characterIcon.sprite = model.CharacterSprite;
            _playerName.text = model.PlayerName;
            _playerScore.text = model.PlayerScore.ToString();
            _playerDamageDealt.text = model.PlayerDamageDealt.ToString();
            _playerDamageReceived.text = model.PlayerDamageReceived.ToString();
            _playerOgreCount.text = model.PlayerOgreCount.ToString();
            _playerExhibitsInteractCount.text = model.PlayerExhibitsInteractCount.ToString();
        }
    }
}