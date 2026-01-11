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

            // リザルト情報から演出とUI表示に必要なデータを取得
            var result = gameResultInfo.Ranking
                .OrderBy(x => x.Rank)
                .Join(
                    gameResultInfo.Players,
                    r => r.PlayerName,
                    p => p.PlayerName,
                    (r, p) => (r.Rank, r.PlayerName, p.CharacterType))
                .ToArray();
            
            // ランキング表示用モデル
            var rankingListNames = 
                result.Where(x => x.Rank >= 2)
                    .Select(x => new RankingItemModel(x.Rank, x.PlayerName, x.CharacterType))
                    .ToArray();
            
            _rankingView.CreateRankingList(rankingListNames);
            _rankingView.SetWinnerPlayerNameText(gameResultInfo.Ranking.FirstOrDefault(x => x.Rank == 1).PlayerName);

            // アセットの取得
            Debug.Log("Get Asset Process");
            Debug.Log(gameResultInfo.StageSceneName);

            var winner = result.FirstOrDefault(r => r.Rank == 1);
            var winnerAssets = resultCharacterDataContainer.GetAssets(winner.CharacterType);
            var winnerPrefab = winnerAssets.ResultCharacterPrefab;

            // 演出開始
            var loadSceneTask = UniTask.Create(gameResultInfo.StageSceneName, async stageSceneName =>
            {
                var loadSceneName = "Result_" + stageSceneName;
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