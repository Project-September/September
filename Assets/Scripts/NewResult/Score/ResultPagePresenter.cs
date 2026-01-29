using NewResult.UI;

namespace September.NewResult
{
    public class ResultPagePresenter
    {
        private readonly IExhibitScoreView _exhibitScoreView;
        private readonly ITotalScoreView _totalScoreView;
        private readonly IResultDetailsView _resultDetailsView;
        private readonly ResultCharacterDataContainer _resultCharacterDataContainer;

        public ResultPagePresenter(
            IExhibitScoreView exhibitScoreView, 
            ITotalScoreView totalScoreView,
            IResultDetailsView resultDetailsView,
            ResultCharacterDataContainer resultCharacterDataContainer)
        {
            _exhibitScoreView = exhibitScoreView;
            _totalScoreView = totalScoreView;
            _resultDetailsView = resultDetailsView;
            _resultCharacterDataContainer = resultCharacterDataContainer;
        }
        
        public void Update(GameResultInfo gameResultInfo)
        {
            _exhibitScoreView?.Setup(gameResultInfo.Players[0].ExhibitScoreEntries);

            var totalScoreViewEntries = new TotalScoreViewEntry[gameResultInfo.Players.Count];
            var playerDetailsModels = new PlayerDetailsModel[gameResultInfo.Players.Count];
            for (int i = 0; i < gameResultInfo.Players.Count; i++)
            {
                var player = gameResultInfo.Players[i];
                var assets = _resultCharacterDataContainer.GetAssets(player.CharacterType);
                var sprite = assets.Icon;
                var detailSprite = assets.ResultDetailViewIcon;
                var name = player.PlayerName;
                var isOgre = player.IsOgre;
                var isSelf = player.IsSelf;
                
                var score = player.TotalScore;
                totalScoreViewEntries[i] = new TotalScoreViewEntry(sprite, name, score, isOgre, isSelf);
                playerDetailsModels[i] = new PlayerDetailsModel()
                {
                    CharacterSprite = detailSprite, 
                    PlayerName = name, 
                    PlayerScore = score,
                    PlayerDamageDealt = player.DamageDealt, 
                    PlayerDamageReceived = player.DamageReceived,
                    PlayerExhibitsInteractCount = player.ExhibitInteractCount,
                    PlayerOgreCount = player.OgreCount,
                };
            }
            _totalScoreView?.Setup(totalScoreViewEntries);
            _resultDetailsView?.Setup(playerDetailsModels);
        }
    }
}