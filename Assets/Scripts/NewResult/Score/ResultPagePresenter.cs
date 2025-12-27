using System;
using UnityEngine;

namespace September.NewResult
{
    public class ResultPagePresenter
    {
        private readonly IExhibitScoreView _exhibitScoreView;
        private readonly ITotalScoreView _totalScoreView;
        private readonly ResultCharacterDataContainer _resultCharacterDataContainer;

        public ResultPagePresenter(
            IExhibitScoreView exhibitScoreView, 
            ITotalScoreView totalScoreView,
            ResultCharacterDataContainer resultCharacterDataContainer)
        {
            _exhibitScoreView = exhibitScoreView;
            _totalScoreView = totalScoreView;
            _resultCharacterDataContainer = resultCharacterDataContainer;
        }
        
        public void Update(GameResultInfo gameResultInfo)
        {
            _exhibitScoreView?.Setup(gameResultInfo.Players[0].ExhibitScoreEntries);

            var totalScoreViewEntries = new TotalScoreViewEntry[gameResultInfo.Players.Count];
            for (int i = 0; i < gameResultInfo.Players.Count; i++)
            {
                var player = gameResultInfo.Players[i];
                var sprite = _resultCharacterDataContainer.GetAssets(player.CharacterType).Icon;
                var name = player.PlayerName;
                var isOgre = player.IsOgre;
                
                var score = player.TotalScore;
                totalScoreViewEntries[i] = new TotalScoreViewEntry(sprite, name, score, isOgre);
            }
            _totalScoreView?.Setup(totalScoreViewEntries);
        }
    }
}