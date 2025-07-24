using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendAttackState : IFriendState
    {
        public void OnEnter(FriendBase friend)
        {
            friend.Agent.isStopped = true; //Navmeshを止める
            friend.Animator?.SetTrigger("Attack"); // アニメーターにAttackトリガーがある前提
        }

        public void OnExit(FriendBase friend)
        {
            //Navmeshを再開
            friend.Agent.isStopped = false;
        }

        public void OnUpdate(FriendBase friend)
        {
            
        }
    }
}