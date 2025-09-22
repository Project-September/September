using Fusion;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendAnimationController : NetworkBehaviour
    {
        [SerializeField] private Animator _animator;

        public void PlayAnimation(string animationName)
        {
            RPC_PlayAnimation(animationName);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayAnimation(string animationName)
        {
            _animator.Play(animationName);
        }
    }
}