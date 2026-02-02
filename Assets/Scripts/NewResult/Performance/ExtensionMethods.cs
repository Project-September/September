using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace September.NewResult
{
    public static class ExtensionMethods
    {
        public static async UniTask PlayAsync(this Animator animator, string animationName)
        {
            animator.Play(animationName);
            await UniTask.DelayFrame(1);
            await WaitUntilEndState(animator, animationName);
        }

        public static async UniTask WaitUntilEndState(this Animator animator, string stateName)
        {
            await UniTask.WaitUntil(
                (animator, stateName), 
                static state =>
                {
                    var info = state.animator.GetCurrentAnimatorStateInfo(0);
                    return !info.IsName(state.stateName) || info.normalizedTime >= 1f;
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
        
        public static void PlayInstant(this Animator animator, AnimationClip clip)
        {
            var graph = PlayableGraph.Create("IdlePlayableGraph");
            var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
                    
            var clipPlayable = AnimationClipPlayable.Create(graph, clip);
            output.SetSourcePlayable(clipPlayable);
                    
            graph.Play();
        }
        
        public static string ToFieldName(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            value = value.TrimStart('_');
            value = char.ToUpper(value[0]) + value[1..];
            return value;
        }
    }
}