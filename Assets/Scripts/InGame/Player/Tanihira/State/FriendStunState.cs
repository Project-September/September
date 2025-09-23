using Ingame.Tanihira;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendStunState : IFriendState
    {
        private float _stunTime;
        private float _stunTimer;
        private bool _isStun;
        private FriendAnimationController _friendAnimationController;
    
        public void OnEnter(FriendBase friend)
        {
            if (!_friendAnimationController)
            {
                _friendAnimationController = friend.GetComponent<FriendAnimationController>();
            }
            _stunTime = friend.CurrentFriendStatus.FriendStunTime;
            _stunTimer = 0;
            _isStun = true;
            //隊列から離れる
            friend.FormationManager?.DeleteFriend(friend);
            if (_friendAnimationController)
            {
                //スタン時のアニメーション
                _friendAnimationController.PlayAnimation("StunStart");
            }
        }

        public void OnExit(FriendBase friend)
        {
            
        }

        public void OnUpdate(FriendBase friend)
        {
            _stunTimer += friend.Runner.DeltaTime;
            //スタン時間が終わった後の処理
            if (_stunTimer >= _stunTime && _isStun)
            {
                if (_friendAnimationController)
                {
                    _friendAnimationController.PlayAnimation("Getup");
                }
                _isStun = false;
            }
        }
    }
}