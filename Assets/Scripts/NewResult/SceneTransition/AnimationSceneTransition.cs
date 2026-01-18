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
        
        protected override async UniTask FadeInPanel()
        {
            await PlayAsync(_openingCoveredStateName);
            State = TransitionState.Opening;
            await PlayAsync(_openingStateName);
            if (!string.IsNullOrEmpty(_completedStateName)) _animator.Play(_completedStateName);
            State = TransitionState.Opened;
        }

        protected override async UniTask FadeOutPanel()
        {
            State = TransitionState.Closing;
            await PlayAsync(_closingStateName);
            State = TransitionState.Covered;
            await PlayAsync(_closingCoveredStateName);
            if (!string.IsNullOrEmpty(_holdStateName)) _animator.Play(_holdStateName);
        }

        public override void SetCovered()
        {
            _animator.Play(_holdStateName);
            State = TransitionState.Covered;
        }

        private async UniTask PlayAsync(string stateName)
        {
            if (!string.IsNullOrEmpty(stateName))
            {
                await _animator.PlayAsync(stateName);
            }
        }

        private async UniTask WaitState(string stateName)
        {
            if (!string.IsNullOrEmpty(stateName))
            {
                await _animator.WaitState(stateName);
            }
        }
    }
}