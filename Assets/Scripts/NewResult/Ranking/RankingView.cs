using September.Common;
using TMPro;
using UnityEngine;

namespace September.NewResult
{
    public class RankingView : MonoBehaviour
    {
        [SerializeField] private Transform _rankingRoot;
        [SerializeField] private RankingItemView _rankingItemPrefab;
        [SerializeField] private TextMeshProUGUI _winnerPlayerNameText;
        [SerializeField] private RankIconContainer _rankIconContainer;
        [SerializeField] private ResultCharacterDataContainer _resultCharacterDataContainer;
        
        public void CreateRankingList(RankingItemModel[] players)
        {
            foreach (Transform child in _rankingRoot)
            {
                Destroy(child.gameObject);
            }
            
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                var rankIcon = _rankIconContainer.GetRankIcon(player.Rank);
                var characterIcon = _resultCharacterDataContainer.GetAssets(player.Type).Icon;
                
                var item = Instantiate(_rankingItemPrefab, _rankingRoot);
                item.Init(rankIcon, characterIcon, player.PlayerName);
            }
        }

        public void SetWinnerPlayerNameText(string playerName)
        {
            _winnerPlayerNameText.text = playerName;
        }
    }

    public readonly struct RankingItemModel
    {
        public readonly int Rank;
        public readonly string PlayerName;
        public readonly CharacterType Type;

        public RankingItemModel(int rank, string playerName, CharacterType type)
        {
            Rank = rank;
            PlayerName = playerName;
            Type = type;
        }
    }
}