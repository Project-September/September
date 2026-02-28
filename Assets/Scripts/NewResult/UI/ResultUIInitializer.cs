using UnityEngine;

namespace September.NewResult
{
    public class ResultUIInitializer : MonoBehaviour
    {
        [SerializeField] private PlayerDetailsView _playerDetailsView;
        [SerializeField] private RankingView _rankingView;
        [SerializeField] private WinnerView _winnerView;
        [SerializeField] private PageController _pageController;
        
        private ResultScenePresenter _resultScenePresenter;

        public void Initialize(ResultCharacterAssetsContainer resultCharacterAssetsContainer, GameResultInfo gameResultInfo)
        {
            _resultScenePresenter = new ResultScenePresenter(
                gameResultInfo,
                resultCharacterAssetsContainer,
                _playerDetailsView,
                _rankingView,
                _winnerView);
            _resultScenePresenter.Refresh();
            
            _pageController.Init();
        }
    }
}