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

        [SerializeField] private float _uiAnimationStartTime = 4.4f;
        
        [Header("決めポーズ中の停止/スローモーション設定")]
        [SerializeField] private float _stopTimeScale = 0.1f;
        [SerializeField] private float _stopTimeDuration = 1.0f;
        
        public async UniTask StartResultPerformance(ResultCharacterAssetsContainer resultCharacterAssetsContainer, GameResultInfo gameResultInfo)
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
            var winnerAssets = resultCharacterAssetsContainer.GetAssets(winner.CharacterType);
            var winnerPrefab = winnerAssets.ResultCharacterPrefab;

            // リザルト用のステージをロード
            var loadSceneTask = UniTask.Create(gameResultInfo.StageSceneName, async stageSceneName =>
            {
                var loadSceneName = "Result_" + stageSceneName;
                await SceneManager.LoadSceneAsync(loadSceneName, LoadSceneMode.Additive);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(loadSceneName));
            });

            _transitionEffect.SetHoldState();
            await loadSceneTask;
            
            // 演出開始
            if (winnerPrefab != null)
            {
                var state = Instantiate(winnerPrefab);
                _transitionEffect.TryTransitionIn().Forget();
                await _transitionEffect.WaitUntilState(TransitionState.Opening);
                state.Play();
                ShowResultUIAsync().Forget();
                await state.WaitFinish();
                Time.timeScale = _stopTimeScale;
                await UniTask.Delay((int)(_stopTimeDuration * _stopTimeScale * 1000), DelayType.DeltaTime);
                Time.timeScale = 1f;
            }
            else
            {
                await _transitionEffect.TryTransitionIn();
                _resultUIAnimator.ShowWinner().Forget();
            }
            
            // UI表示
            await _resultUIAnimator.ShowRankingItems();
            await _resultUIAnimator.ShowMenu();
            Debug.Log("Result Performance End");
            
            // 演出終了⇒メニューを選択可能に
            _menuActiveController.Activate();
            _menuActiveController.SetEventSystemSelected();
        }

        private async UniTask ShowResultUIAsync()
        {
            await UniTask.Delay((int)(_uiAnimationStartTime * 1000));
            await _resultUIAnimator.ShowWinner();
        }
    }
}