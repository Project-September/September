using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public static class ExtensionMethods
    {
        private readonly struct AnimatorStateInfo
        {
            public readonly Animator Animator;
            public readonly string StateName;

            public AnimatorStateInfo(Animator animator, string stateName)
            {
                Animator = animator;
                StateName = stateName;
            }
        }
        
        public static async UniTask PlayAsync(this Animator animator, string animationName)
        {
            animator.Play(animationName);
            await UniTask.WaitUntil(
                new AnimatorStateInfo(animator, animationName), 
                static state =>
                {
                    var info = state.Animator.GetCurrentAnimatorStateInfo(0);
                    return info.IsName(state.StateName) && info.normalizedTime >= 1f ||
                           !info.IsName(state.StateName) && info.normalizedTime is < 1f and > 0f;
                });
        }

        public static async UniTask WaitState(this Animator animator, string stateName)
        {
            await UniTask.WaitUntil((animator, stateName), c =>
            {
                var info = c.animator.GetCurrentAnimatorStateInfo(0);
                return info.IsName(c.stateName);
            });
        }

        public static bool IsPlaying(this Animator animator, string stateName)
        {
            return !string.IsNullOrEmpty(stateName) && animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
        }
    }
}