using TMPro;
using UnityEngine;

namespace September.NewResult
{
    public class RankingView : MonoBehaviour
    {
        [SerializeField] private Transform _rankingRoot;
        [SerializeField] private RankingItemView _rankingItemPrefab;
        [SerializeField] private TextMeshProUGUI _winnerPlayerNameText;
        
        public void CreateRankingList(string[] playerNames)
        {
            for (int i = _rankingRoot.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(_rankingRoot.GetChild(i).gameObject);
            }
            
            for (int i = 0; i < playerNames.Length; i++)
            {
                int rank = i + 2;
                
                var item = Instantiate(_rankingItemPrefab, _rankingRoot);
                item.Init(rank, playerNames[i]);
            }
        }

        public void SetWinnerPlayerNameText(string playerName)
        {
            _winnerPlayerNameText.text = playerName;
        }
    }
}