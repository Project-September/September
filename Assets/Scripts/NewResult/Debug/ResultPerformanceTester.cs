using CRISound;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace September.NewResult
{
    public class ResultPerformanceTester : MonoBehaviour
    {
        [SerializeField] private ResultUIAnimator _resultUIAnimator;
        [SerializeField] private ResultPerformanceState _performanceState;
        [SerializeField] private SceneTransitionEffect _transitionEffect;
        
        [SerializeField, Expandable] private ResultPerformanceSettings _resultPerformanceSettings;
        
        private void Start()
        {
            if (!_performanceState) return;

            if (!_performanceState.gameObject.activeInHierarchy)
            {
                _performanceState = FindFirstObjectByType<ResultPerformanceState>(FindObjectsInactive.Exclude);
            }
            
            PlayResultPerformanceAsync().Forget();
        }

        private async UniTask PlayResultPerformanceAsync()
        {
            _transitionEffect.SetHoldState();
            _transitionEffect.TryTransitionIn().Forget();
            await _transitionEffect.WaitUntilState(TransitionState.Opening);
            
            _performanceState.Play();
            PlayBGM().Forget();
            ShowResultUIAsync().Forget();
            await _performanceState.WaitFinish();
            if (!_performanceState.gameObject) return;
            
            await _resultPerformanceSettings.PlaySlowMotion();
            
            await _resultUIAnimator.ShowRankingItems();
            await _resultUIAnimator.ShowMenu();
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