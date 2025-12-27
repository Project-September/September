using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace September.NewResult
{
    public class TotalScoreEntryView : MonoBehaviour
    {
        [SerializeField] private Image _characterIcon;
        [SerializeField] private TextMeshProUGUI _playerNameText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        
        public void Setup(TotalScoreViewEntry entry)
        {
            _characterIcon.sprite = entry.Icon;
            _playerNameText.text = entry.PlayerName;
            _scoreText.text = entry.Score.ToString();

            var textColor = entry.IsOgre ? Color.red : Color.white;
            _playerNameText.color = textColor;
            _scoreText.color = textColor;
        }
    }
}