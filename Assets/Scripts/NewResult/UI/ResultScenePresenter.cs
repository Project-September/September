using System.Linq;

namespace September.NewResult
{
    public class ResultScenePresenter
    {
        private readonly GameResultInfo _gameResultInfo;
        private readonly ResultCharacterAssetsContainer _resultCharacterAssetsContainer;
        private readonly IPlayerDetailsView _playerDetailsPanel;
        private readonly RankingView _rankingView;
        private readonly WinnerView _winnerView;

        public ResultScenePresenter(
            GameResultInfo gameResultInfo,
            ResultCharacterAssetsContainer resultCharacterAssetsContainer,
            IPlayerDetailsView playerDetailsPanel,
            RankingView rankingView,
            WinnerView winnerView)
        {
            _gameResultInfo = gameResultInfo;
            _playerDetailsPanel = playerDetailsPanel;
            _resultCharacterAssetsContainer = resultCharacterAssetsContainer;
            _rankingView = rankingView;
            _winnerView = winnerView;
        }
        
        public void Refresh()
        {
            var playerDetailsModels = new PlayerDetailsModel[_gameResultInfo.Players.Count];
            var rankingItemModels = new RankingItemModel[_gameResultInfo.Players.Count - 1];
            for (int i = 0; i < _gameResultInfo.Players.Count; i++)
            {
                var player = _gameResultInfo.Players[i];
                var assets = _resultCharacterAssetsContainer.GetAssets(player.CharacterType);
                var sprite = assets.Icon;
                var detailSprite = assets.ResultDetailViewIcon;
                var name = player.PlayerName;
                var isOgre = player.IsOgre;
                var isSelf = player.IsSelf;
                var type = player.CharacterType;
                var rank = _gameResultInfo.Players.First(x => x.PlayerName == player.PlayerName).Rank;
                
                var score = player.TotalScore;
                playerDetailsModels[i] = new PlayerDetailsModel()
                {
                    CharacterSprite = detailSprite, 
                    PlayerName = name, 
                    PlayerScore = score,
                    PlayerDamageDealt = player.DamageDealt, 
                    PlayerDamageReceived = player.DamageReceived,
                    PlayerExhibitsInteractCount = player.ExhibitInteractCount,
                    PlayerOgreCount = player.OgreCount,
                    IsOgre = isOgre,
                    Rank = rank,
                };

                if (i != 0)
                {
                    rankingItemModels[i - 1] = new RankingItemModel(rank, name, type);
                }
            }
            _playerDetailsPanel.Setup(playerDetailsModels);
            _rankingView.Setup(rankingItemModels);
            _winnerView.SetWinnerName(_gameResultInfo.Players.FirstOrDefault(x => x.Rank == 1).PlayerName);
        }
    }
}