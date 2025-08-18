using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendAttackState : IFriendState
    {
        private Transform friendObject;
        public void OnEnter(FriendBase friend)
        {
            //navmeshでの処理
            friend.Agent.isStopped = true;
            friendObject = friend.Agent.gameObject.transform;
            LookTarget(friend.Agent.destination);
            friend.MecanimAnimator?.SetTrigger("Attack"); // アニメーターにAttackトリガーがある前提
        }

        public void OnExit(FriendBase friend)
        {
            //Navmeshを再開
            friend.Agent.isStopped = false;
        }

        public void OnUpdate(FriendBase friend)
        {
            
        }

        private void LookTarget(Vector3 target)
        {
            Vector3 direction = target - friendObject.position;
            direction.y = 0f; // 上下方向は無視

            if (direction.sqrMagnitude > 0.01f)
            {
                friendObject.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}