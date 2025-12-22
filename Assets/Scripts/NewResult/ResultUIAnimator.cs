using Cysharp.Threading.Tasks;
using September.Common;
using UnityEngine;

namespace NewResult
{
    public class ResultUIAnimator : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _animationName;
        
        public async UniTask ShowResultUI()
        {
            _animator.Play(_animationName);
            await UniTask.WaitUntil(
                new AnimatorStateInfo(_animator, _animationName), 
                static state =>
                {
                    var info = state.Animator.GetCurrentAnimatorStateInfo(0);
                    return info.IsName(state.StateName) && info.normalizedTime >= 1f;
                });
        }

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
    }
}