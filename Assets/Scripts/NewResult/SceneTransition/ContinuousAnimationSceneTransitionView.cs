using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public class ContinuousAnimationSceneTransitionView : SceneTransitionView
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _closeStateName;
        [SerializeField] private string _openStateName;
        [SerializeField] private string _completedStateName;

        public override TransitionState State { get; protected internal set; }

        private void Update()
        {
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(_closeStateName))
            {
                State = TransitionState.Closing;
            }
            else if (info.IsName(_openStateName))
            {
                State = TransitionState.Opening;
            }
            else if (info.IsName(_completedStateName))
            {
                State = TransitionState.Opened;
            }
            else
            {
                State = TransitionState.Covered;
            }
        }

        protected override async UniTask FadeInPanel(UniTask loadingTask)
        {
            if (_animator.IsPlaying(_completedStateName))
            {
                await _animator.PlayAsync(_openStateName);
            }
            else
            {
                await _animator.WaitState(_completedStateName);
            }
        }

        protected override async UniTask FadeOutPanel(UniTask loadingTask)
        {
            await _animator.PlayAsync(_closeStateName);
        }
    }
}