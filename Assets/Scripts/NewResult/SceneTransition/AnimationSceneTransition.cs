using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public class AnimationSceneTransition : SceneTransitionView
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _defaultStateName;          // 初期状態・未開始
        [SerializeField] private string _closingStateName;          // 閉幕～画面を完全に覆うまで
        [SerializeField] private string _closingCoveredStateName;   // 閉幕～静止
        [SerializeField] private string _holdStateName;             // 静止状態
        [SerializeField] private string _openingCoveredStateName;   // 開幕～画面を完全に覆わなくなるまで
        [SerializeField] private string _openingStateName;          // 開幕
        [SerializeField] private string _completedStateName;        // 完了

        public override TransitionState State { get; protected internal set; }
        
        protected override async UniTask FadeInPanel(UniTask loadingTask)
        {
            _animator.Play(_holdStateName);
            await loadingTask;
            await _animator.PlayAsync(_openingCoveredStateName);
            State = TransitionState.Opening;
            await _animator.WaitState(_completedStateName);
            State = TransitionState.Opened;
        }

        protected override async UniTask FadeOutPanel(UniTask loadingTask)
        {
            State = TransitionState.Closing;
            await _animator.PlayAsync(_closingStateName);
            State = TransitionState.Covered;
            await _animator.WaitState(_holdStateName);
            await loadingTask;
        }
    }
}