using Cysharp.Threading.Tasks;
using September.Common;
using UnityEngine;

namespace September.NewResult
{
    public class ResultUIAnimator : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _animationName;
        
        public async UniTask ShowResultUI()
        {
            await _animator.PlayAsync(_animationName);
        }
    }
}