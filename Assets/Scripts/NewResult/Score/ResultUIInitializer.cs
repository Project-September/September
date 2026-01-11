using UnityEngine;
using UnityEngine.Serialization;

namespace September.NewResult
{
    public class ResultUIInitializer : MonoBehaviour
    {
        [SerializeField] private ExhibitScoreView _exhibitScoreView;
        [SerializeField] private TotalScoreView _totalScoreView;
        [SerializeField] private ResultCharacterDataContainer _resultCharacterDataContainer;
        [SerializeField] private PageController _pageController;
        
        private ResultPagePresenter _resultPagePresenter;

        public void Initialize(GameResultInfo gameResultInfo)
        {
            _resultPagePresenter = new ResultPagePresenter(
                _exhibitScoreView,
                _totalScoreView,
                _resultCharacterDataContainer);
            _resultPagePresenter.Update(gameResultInfo);
            
            _pageController.Init();
        }
    }
}