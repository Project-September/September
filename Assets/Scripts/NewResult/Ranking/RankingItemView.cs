using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace September.NewResult
{
    public class RankingItemView : MonoBehaviour
    {
        [SerializeField] private Image _rankIcon;
        [SerializeField] private Image _characterIcon;
        [SerializeField] private TextMeshProUGUI _playerNameText;
        
        public void Init(Sprite rankIcon, Sprite characterIcon, string playerName)
        {
            _rankIcon.sprite = rankIcon;
            _characterIcon.sprite = characterIcon;
            _playerNameText.text = playerName;
        }
    }
}