using Ingame.Tanihira;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendWaitState : IFriendState
    {
        private float _waitTime = 0.5f;
        private float _waitTimer;
    
        public void OnEnter(FriendBase friend)
        {
            _waitTimer = 0;
        }

        public void OnExit(FriendBase friend)
        {
            friend.Agent.enabled = true;
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