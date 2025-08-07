using UnityEngine;
using UnityEngine.AI;

namespace Ingame.Tanihira
{
    public class FriendMoveState : IFriendState
    {
        public void OnEnter(FriendBase friend)
        {
            if (!friend.Agent.isOnNavMesh)
            {
                Debug.LogWarning("AgentはまだNavMesh上にいません");
                return;
            }
            
            //目的地を設定
            if (friend.Destination != null)
                friend.Agent.SetDestination(friend.Destination.position);
            
            //agentが移動できるように設定
            friend.Agent.enabled = true;
            friend.Agent.isStopped = false;
            friend.Agent.updatePosition = true;
            friend.Agent.updateRotation = true;
            
            //移動時のステータスを設定
            friend.Agent.speed = friend.FriendStatus.FriendFormationSpeed;
        }

        public void OnExit(FriendBase friend)
        {
            
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