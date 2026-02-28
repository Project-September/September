using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace September.NewResult
{
    public class ResultPerformanceManager : MonoBehaviour
    {
        [SerializeField] private ResultPerformanceHandler _handler;
        
        public async UniTask StartResultPerformance(ResultCharacterAssetsContainer resultCharacterAssetsContainer, GameResultInfo gameResultInfo)
        {
            // リザルト用のステージをロード
            await LoadResultScene(gameResultInfo);
            
            // リザルトキャラクターを生成して取得
            var state = GetState(resultCharacterAssetsContainer, gameResultInfo);
            
            // リザルト演出の再生
            await _handler.Play(state, destroyCancellationToken);
        }

        private static async UniTask LoadResultScene(GameResultInfo gameResultInfo)
        {
            var loadSceneTask = UniTask.Create(gameResultInfo.StageSceneName, async stageSceneName =>
            {
                var loadSceneName = "Result_" + stageSceneName;
                await SceneManager.LoadSceneAsync(loadSceneName, LoadSceneMode.Additive);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(loadSceneName));
            });

            await loadSceneTask;
        }

        private static ResultPerformanceState GetState(ResultCharacterAssetsContainer resultCharacterAssetsContainer, GameResultInfo gameResultInfo)
        {
            ResultPerformanceState state;
            {
                var winner = gameResultInfo.Players.FirstOrDefault(r => r.Rank == 1);
                var winnerAssets = resultCharacterAssetsContainer.GetAssets(winner.CharacterType);
                var winnerPrefab = winnerAssets.ResultCharacterPrefab;

                if (winnerPrefab == null)
                {
                    Debug.LogError($"Result character {winner.CharacterType} assets contains no winner prefab.", resultCharacterAssetsContainer);
                    return null;
                }
                
                state = Instantiate(winnerPrefab);
            }
            return state;
        }
    }
}