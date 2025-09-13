using UnityEngine;
using UnityEngine.AI;

namespace Ingame.Tanihira
{
    public class FriendMoveState : IFriendState
    {
        private bool _isSetDestination;
        public void OnEnter(FriendBase friend)
        {
            if (!friend.Agent.isOnNavMesh)
            {
                Debug.LogWarning("AgentはまだNavMesh上にいません");
                return;
            }
            
            //agentが移動できるように設定
            friend.Agent.enabled = true;
            friend.Agent.isStopped = false;
          
            //移動時のステータスを設定
            friend.Agent.speed = friend.CurrentFriendStatus.FriendFormationSpeed;
            friend.Agent.stoppingDistance = friend.CurrentFriendStatus.FriendFormationDistance;
        }

        public void OnExit(FriendBase friend)
        {
            _isSetDestination = false;
        }

        public void OnUpdate(FriendBase friend)
        {
            if (friend.Destination == null || !friend.Agent.isOnNavMesh) return;

            friend.Agent.SetDestination(friend.Destination.position);
            //速度に応じて、アニメーションを変化させる
            friend.Animator.SetFloat("MoveBlend", friend.Agent.velocity.magnitude);
        }
    }
}