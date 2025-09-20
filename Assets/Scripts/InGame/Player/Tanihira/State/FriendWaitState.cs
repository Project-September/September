using Ingame.Tanihira;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendWaitState : IFriendState
    {
        private float _waitTime = 1.0f;
        private float _waitTimer;
    
        public void OnEnter(FriendBase friend)
        {
            _waitTimer = 0;
        }

        public void OnExit(FriendBase friend)
        {
            friend.Agent.enabled = true;
            //friend.Agent.isStopped = false;
        }

        public void OnUpdate(FriendBase friend)
        {
            _waitTimer += Time.deltaTime;
            if (_waitTimer >= _waitTime)
            {
                friend.FinishWaitTime();
            }
        }
    }
}