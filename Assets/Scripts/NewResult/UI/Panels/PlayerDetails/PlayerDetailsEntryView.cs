using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace September.NewResult
{
    public class PlayerDetailsEntryView : MonoBehaviour
    {
        [SerializeField] private Image _characterIcon;
        [SerializeField] private Image _ogreIcon;
        [SerializeField] private Image _winDisplayIcon;
        [SerializeField] private Image _defaultRankIcon;
        [SerializeField] private RankIconContainer _rankIconContainer;
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
            _ogreIcon.gameObject.SetActive(model.IsOgre);
            _winDisplayIcon.gameObject.SetActive(model.Rank == 1);
            _defaultRankIcon.gameObject.SetActive(model.Rank != 1 && !model.IsOgre);

            if (model.Rank != 1 || !model.IsOgre)
            {
                _defaultRankIcon.sprite = _rankIconContainer.GetRankIcon(model.Rank);
            }
        }
    }
}