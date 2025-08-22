using Ingame.Tanihira;
using UnityEngine;
using UnityEngine.UI;

public class FriendChaseState : IFriendState
{
    public void OnEnter(FriendBase friend)
    {
        //目的地を設定
        if (friend.Destination != null)
            friend.Agent.SetDestination(friend.Destination.position);
        
        friend.Agent.speed = friend.FriendStatus.FriendChaseSpeed;
        friend.Agent.stoppingDistance = friend.FriendStatus.FriendChaseDistance;
    }

    public void OnExit(FriendBase friend)
    {
        
    }

    public void OnUpdate(FriendBase friend)
    {
        if (friend.Destination == null || !friend.Agent.isOnNavMesh) return;
            
        //速度に応じて、アニメーションを変化させる
        friend.Animator.SetFloat("MoveBlend", friend.Agent.velocity.magnitude);

        if (friend.Agent.remainingDistance <= friend.Agent.stoppingDistance && !friend.IsAttack)
        {
            friend.ChangeState(FriendState.Attack);
        }
    }
}
