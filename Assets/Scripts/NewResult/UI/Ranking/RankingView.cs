using September.Common;
using UnityEngine;

namespace September.NewResult
{
    public class RankingView : MonoBehaviour
    {
        [SerializeField] private Transform _rankingRoot;
        [SerializeField] private RankingItemView _rankingItemPrefab;
        [SerializeField] private RankIconContainer _rankIconContainer;
        [SerializeField] private ResultCharacterAssetsContainer _resultCharacterAssetsContainer;
        
        public void Setup(RankingItemModel[] players)
        {
            foreach (Transform child in _rankingRoot)
            {
                Destroy(child.gameObject);
            }
            
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                var rankIcon = _rankIconContainer.GetRankIcon(player.Rank);
                var characterIcon = _resultCharacterAssetsContainer.GetAssets(player.Type).Icon;
                
                var item = Instantiate(_rankingItemPrefab, _rankingRoot);
                item.Init(rankIcon, characterIcon, player.PlayerName);
            }
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