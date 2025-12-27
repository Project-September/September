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
        
        public void Setup(Sprite icon, string playerName, int score)
        {
            _characterIcon.sprite = icon;
            _playerNameText.text = playerName;
            _scoreText.text = score.ToString();
        }
    }
}