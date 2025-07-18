using UnityEngine;
using UnityEngine.AI;

namespace Ingame.Tanihira
{
    public class FriendMoveState : FriendStateBase
    {
        public override void OnEnter()
        {
            //目的地を設定
            if (_owner.Destination != null)
                _owner.Agent.SetDestination(_owner.Destination.position);
            
            //agentが移動できるように設定
            _owner.Agent.isStopped = false;
            _owner.Agent.updatePosition = true;
            _owner.Agent.updateRotation = true;
        }

        public override void OnExit()
        {
            
        }

        public override void OnUpdate()
        {
            if (_owner.Destination == null || !_owner.Agent.isOnNavMesh) return;

            if (_owner.Destination.position != _owner.Agent.destination)
            {
                _owner.Agent.SetDestination(_owner.Destination.position);
            }
            
            Debug.Log($"Speed: {_owner.Agent.speed}, Accel: {_owner.Agent.acceleration}");
        }
    }
}