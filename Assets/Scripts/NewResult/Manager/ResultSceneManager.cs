using UnityEngine;
using UnityEngine.Serialization;

namespace September.NewResult
{
    public class ResultSceneManager : MonoBehaviour
    {
        [SerializeField] private ResultCharacterAssetsContainer _resultCharacterAssetsContainer;
        [SerializeField] private ResultPerformanceManager _resultPerformanceManager;
        [SerializeField] private ResultUIInitializer _resultUIInitializer;

        private async void Start()
        {
            var gameResultInfo = InGameResultContainer.Info;
            
            _resultUIInitializer.Initialize(gameResultInfo);
            
            await _resultPerformanceManager.StartResultPerformance(_resultCharacterAssetsContainer, gameResultInfo);
        }
    }
}