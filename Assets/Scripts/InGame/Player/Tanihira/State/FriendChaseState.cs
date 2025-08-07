using Ingame.Tanihira;
using UnityEngine;
using UnityEngine.UI;

public class FriendChaseState : IFriendState
{
    private float _stopDistance;
    
    public void OnEnter(FriendBase friend)
    {
        
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

        if (friend.Agent.remainingDistance <= _stopDistance)
        {
            friend.ChangeState(FriendState.Attack);
        }
    }
}
