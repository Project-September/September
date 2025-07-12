using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendMoveState : FriendStateBase
    {
        public override void OnEnter()
        {
            //目的地を設定
            if (_owner.Destination != null)
                _owner.Agent.SetDestination(_owner.Destination.position);
        }

        public override void OnExit()
        {
            
        }

        public override void OnUpdate()
        {
            if (_owner.Destination == null || _owner.Agent.isOnNavMesh) return;

            _owner.Agent.SetDestination(_owner.Destination.position);
        }
    }
}