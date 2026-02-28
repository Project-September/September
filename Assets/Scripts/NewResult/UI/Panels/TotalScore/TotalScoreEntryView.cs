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
        [SerializeField] private Image _isSelfBar;
        
        public void Setup(TotalScoreModel model)
        {
            _characterIcon.sprite = model.Icon;
            _playerNameText.text = model.PlayerName;
            _scoreText.text = model.Score.ToString();

            var textColor = model.IsOgre ? Color.red : Color.white;
            _playerNameText.color = textColor;
            _scoreText.color = textColor;
            
            _isSelfBar.gameObject.SetActive(model.IsSelf);
        }
    }
}