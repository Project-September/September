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

            var totalScoreViewEntries = new (Sprite, string, int)[gameResultInfo.Players.Count];
            for (int i = 0; i < gameResultInfo.Players.Count; i++)
            {
                var player = gameResultInfo.Players[i];
                var sprite = _resultCharacterDataContainer.GetAssets(player.CharacterType).Icon;
                var name = player.PlayerName;
                var score = player.TotalScore;
                totalScoreViewEntries[i] = (sprite, name, score);
            }
            _totalScoreView?.Setup(totalScoreViewEntries);
        }
    }
}