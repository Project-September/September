using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace September.NewResult
{
    public class ResultPerformanceTester : MonoBehaviour
    {
        [SerializeField] private SceneTransitionEffect _transitionEffect;
        [SerializeField] private ResultUIAnimator _resultUIAnimator;
        [SerializeField] private ResultPerformanceState _performanceState;
        
        [SerializeField, Expandable] private ResultPerformanceSettings _resultPerformanceSettings;
        
        private void Start()
        {
            if (!_performanceState) return;
            NewMethod().Forget();
        }

        private async UniTask NewMethod()
        {
            _transitionEffect.SetHoldState();
            _transitionEffect.TryTransitionIn().Forget();
            await _transitionEffect.WaitUntilState(TransitionState.Opening);
            
            _performanceState.Play();
            ShowResultUIAsync().Forget();
            await _performanceState.WaitFinish();

            await _resultPerformanceSettings.PlaySlowMotion();
            
            await _resultUIAnimator.ShowRankingItems();
            await _resultUIAnimator.ShowMenu();
        }
        
        private async UniTask ShowResultUIAsync()
        {
            await UniTask.WaitForSeconds(_resultPerformanceSettings.UIAnimationStartTime);
            await _resultUIAnimator.ShowWinner();
        }
    }
}