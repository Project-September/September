using System.Linq;
using System.Threading;
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
        [SerializeField] private MenuActiveController _menuActiveController;
        [SerializeField] private SceneTransitionEffect _transitionEffect;

        [SerializeField, Expandable] private ResultPerformanceSettings _resultPerformanceSettings;
        
        public async UniTask StartResultPerformance(ResultCharacterAssetsContainer resultCharacterAssetsContainer, GameResultInfo gameResultInfo)
        {
            // リザルト用のステージをロード
            await LoadResultScene(gameResultInfo);
            
            var state = GetState(resultCharacterAssetsContainer, gameResultInfo);
            
            await PlayResult(state);
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
                state = Instantiate(winnerPrefab);
            }
            return state;
        }
        
        private async UniTask PlayResult(ResultPerformanceState state)
        {
            _menuActiveController.Deactivate();
            
            _transitionEffect.SetHoldState();
            _transitionEffect.TryTransitionIn().Forget();
            await _transitionEffect.WaitUntilState(TransitionState.Opening);
            
            // 演出開始
            state?.Play();
            
            PlayBGM().Forget();
            ShowResultUIAsync().Forget();
            
            if (state != null)
            {   
                await state.WaitFinish();
                await _resultPerformanceSettings.PlaySlowMotion();
            }
            
            // UI表示
            await _resultUIAnimator.ShowRankingItems();
            await _resultUIAnimator.ShowMenu();
            Debug.Log("Result Performance End");
            
            // 演出終了⇒メニューを選択可能に
            ShowCursor(destroyCancellationToken).Forget();
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
        
        private static async UniTask ShowCursor(CancellationToken token)
        {
            await UniTask.WaitUntil(() => GameInput.I.UseDeviceType == GameInput.DeviceType.KeyboardMouse, cancellationToken: token);
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}