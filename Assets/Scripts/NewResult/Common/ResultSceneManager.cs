using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public class ResultSceneManager : MonoBehaviour
    {
        [SerializeField] private ResultCharacterAssetsContainer _resultCharacterAssetsContainer;
        [SerializeField] private ResultPerformanceManager _resultPerformanceManager;
        [SerializeField] private ResultUIInitializer _resultUIInitializer;

        private void Start()
        {
            PlayResultPerformance().Forget();
        }

        private async UniTask PlayResultPerformance()
        {
            var gameResultInfo = InGameResultContainer.Info;
            if (gameResultInfo == null)
            {
                Debug.LogError("GameResultInfo is null");
            }
            
            _resultUIInitializer.Initialize(_resultCharacterAssetsContainer, gameResultInfo);
            
            await _resultPerformanceManager.StartResultPerformance(_resultCharacterAssetsContainer, gameResultInfo);
        }
    }
}