using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public class ResultPerformanceTester : MonoBehaviour
    {
        [SerializeField] private SceneTransitionEffect _transitionEffect;
        [SerializeField] private ResultUIAnimator _resultUIAnimator;
        [SerializeField] private ResultPerformanceState _performanceState;
        [SerializeField] private float _uiAnimationStartTime = 4.4f;
        
        [Header("決めポーズ中の停止/スローモーション設定")]
        [SerializeField] private float _stopTimeScale = 0.1f;
        [SerializeField] private float _stopTimeDuration = 1.0f;
        
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
            
            Time.timeScale = _stopTimeScale;
            await UniTask.Delay((int)(_stopTimeDuration * _stopTimeScale * 1000), DelayType.DeltaTime);
            Time.timeScale = 1f;
            
            await _resultUIAnimator.ShowRankingItems();
            await _resultUIAnimator.ShowMenu();
        }
        
        private async UniTask ShowResultUIAsync()
        {
            await UniTask.Delay((int)(_uiAnimationStartTime * 1000));
            await _resultUIAnimator.ShowWinner();
        }
    }
}