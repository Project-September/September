using UnityEngine;
using UnityEngine.AI;

namespace Ingame.Tanihira
{
    public class FriendMoveState : FriendStateBase
    {
        public override void OnEnter()
        {
            if (!_owner.Agent.isOnNavMesh)
            {
                Debug.LogWarning("AgentはまだNavMesh上にいません");
                return;
            }
            
            //目的地を設定
            if (_owner.Destination != null)
                _owner.Agent.SetDestination(_owner.Destination.position);
            
            //agentが移動できるように設定
            _owner.Agent.enabled = true;
            _owner.Agent.isStopped = false;
            _owner.Agent.updatePosition = true;
            _owner.Agent.updateRotation = true;
            
            //移動時のステータスを設定
            _owner.Agent.speed = _status.FriendFormationSpeed; 
        }

        public override void OnExit()
        {
            
        }

        public override void OnUpdate()
        {
            Debug.Log(_owner.Agent.isOnNavMesh);
            if (_owner.Destination == null || !_owner.Agent.isOnNavMesh) return;

            if (_owner.Destination.position != _owner.Agent.destination)
            {
                _owner.Agent.SetDestination(_owner.Destination.position);
            }
            
            //速度に応じて、アニメーションを変化させる
            _owner.Animator.SetFloat("MoveBlend", _owner.Agent.velocity.magnitude);
            Debug.Log(_owner.Agent.velocity.magnitude);
        }
    }
}