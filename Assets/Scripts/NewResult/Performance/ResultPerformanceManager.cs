using System.Linq;
using CRISound;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
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

        [SerializeField, Expandable] private ResultPerformanceSettings _resultPerformanceSettings;
        
        public async UniTask StartResultPerformance(ResultCharacterAssetsContainer resultCharacterAssetsContainer, GameResultInfo gameResultInfo)
        {
            _menuActiveController.Deactivate();

            // リザルト情報から演出とUI表示に必要なデータを取得
            var rankingListNames = gameResultInfo.Players
                .Skip(1) // 最初のプレイヤーは表示しない
                .Select(x => new RankingItemModel(x.Rank, x.PlayerName, x.CharacterType))
                .ToArray();
            
            _rankingView.CreateRankingList(rankingListNames);
            _rankingView.SetWinnerPlayerNameText(gameResultInfo.Players.FirstOrDefault(x => x.Rank == 1).PlayerName);

            // アセットの取得
            Debug.Log("Get Asset Process");

            var winner = gameResultInfo.Players.FirstOrDefault(r => r.Rank == 1);
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
                PlayBGM().Forget();
                ShowResultUIAsync().Forget();
                
                await state.WaitFinish();
                await _resultPerformanceSettings.PlaySlowMotion();
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
            await UniTask.WaitForSeconds(_resultPerformanceSettings.UIAnimationStartTime);
            await _resultUIAnimator.ShowWinner();
        }
        
        private async UniTask PlayBGM()
        {
            await UniTask.WaitForSeconds(_resultPerformanceSettings.BgmStartTime);
            CRIAudio.PlayBGM("ALLCue", "BGM_ResultVictory");
        }
    }
}