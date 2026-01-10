using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace September.NewResult
{
    public class ResultPerformanceManager : MonoBehaviour
    {
        [SerializeField] private ResultUIAnimator _resultUIAnimator;
        [SerializeField] private RankingView _rankingView;
        [SerializeField] private MenuActiveController _menuActiveController;
        [SerializeField] private SceneTransitionEffect _transitionEffect;
        
        public async UniTask StartResultPerformance(ResultCharacterDataContainer resultCharacterDataContainer, GameResultInfo gameResultInfo)
        {
            _menuActiveController.Deactivate();
            
            // ランキングの作成
            var rankingListNames = 
                gameResultInfo.Ranking.Where(x => x.Rank >= 2)
                    .OrderBy(x => x.Rank)
                    .Select(x => new RankingItemModel(x.Rank, x.PlayerName, x.CharacterType))
                    .ToArray();
            
            _rankingView.CreateRankingList(rankingListNames);
            _rankingView.SetWinnerPlayerNameText(gameResultInfo.Ranking.FirstOrDefault(x => x.Rank == 1).PlayerName);

            // アセットの取得
            var ranking = gameResultInfo.Ranking;
            Debug.Log("Get Asset Process");
            Debug.Log(gameResultInfo.StageSceneName);
            foreach (var entry in ranking)
            {
                Debug.Log($"{entry.Rank}, {entry.PlayerName}, {entry.CharacterType}");
            }

            var winner = gameResultInfo.Ranking.FirstOrDefault(r => r.Rank == 1);
            var winnerAssets = resultCharacterDataContainer.GetAssets(winner.CharacterType);
            var winnerPrefab = winnerAssets.ResultCharacterPrefab;

            // 演出開始
            var loadSceneTask = UniTask.Create(async () =>
            {
                var loadSceneName = "Result_" + gameResultInfo.StageSceneName;
                await SceneManager.LoadSceneAsync(loadSceneName, LoadSceneMode.Additive);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(loadSceneName));
            });

            if (winnerPrefab != null)
            {
                var state = Instantiate(winnerPrefab);
                state.Play();
                await _transitionEffect.TryFadeIn(loadSceneTask);
                await state.WaitFinish();
            }
            else
            {
                await _transitionEffect.TryFadeIn(loadSceneTask);
            }
            
            // 演出終了⇒UI表示
            await _resultUIAnimator.ShowResultUI();
            Debug.Log("Result Performance End");
            
            // メニューを選択可能に
            _menuActiveController.Activate();
            _menuActiveController.SetEventSystemSelected();
        }
    }
}