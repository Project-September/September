using System;
using System.Threading;
using CRISound;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace September.NewResult
{
    [Serializable]
    public class ResultPerformanceHandler
    {
        [SerializeField] private ResultUIAnimator _resultUIAnimator;
        [SerializeField] private SceneTransitionEffect _transitionEffect;
        [SerializeField] private MenuActiveController _menuActiveController;
        [SerializeField, Expandable] private ResultPerformanceSettings _settings;
        
        public async UniTask Play(ResultPerformanceState state, CancellationToken token)
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
                await state.WaitFinish(token);
                await _settings.PlaySlowMotion(token);
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
            await UniTask.WaitForSeconds(_settings.UIAnimationStartTime);
            await _resultUIAnimator.ShowWinner();
        }

        private async UniTask PlayBGM()
        {
            await UniTask.WaitForSeconds(_settings.BgmStartTime);
            CRIAudio.PlayBGM("ALLCue", "BGM_ResultVictory");
        }
    }
}