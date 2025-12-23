using TMPro;
using UnityEngine;

namespace NewResult
{
    public class RankingItemView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _playerNameText;
        
        public void Init(int rank, string playerName)
        {
            _rankText.text = rank.ToString();
            _playerNameText.text = playerName;
        }
    }
}