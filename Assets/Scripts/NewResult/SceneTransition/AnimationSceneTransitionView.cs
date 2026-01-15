using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public class AnimationSceneTransitionView : SceneTransitionView
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _openAnimationName;
        [SerializeField] private string _closeAnimationName;

        public override TransitionState State { get; protected internal set; }

        protected override async UniTask FadeInPanel(UniTask loadingTask)
        {
            State = TransitionState.Opening;
            await _animator.PlayAsync(_openAnimationName);
            State = TransitionState.Opened;
        }

        protected override async UniTask FadeOutPanel(UniTask loadingTask)
        {
            State = TransitionState.Closing;
            await _animator.PlayAsync(_closeAnimationName);
            State = TransitionState.Covered;
        }
    }
}