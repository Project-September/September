using UnityEngine;

namespace Ingame.Tanihira
{
    public class FriendAttackState : FriendStateBase
    {
        [SerializeField] private float stopDistance = 3f;

        public override void OnEnter()
        {
            _owner.Agent.isStopped = true; //Navmeshを止める
            _owner.Animator?.SetTrigger("Attack"); // アニメーターにAttackトリガーがある前提
        }

        public override void OnExit()
        {
            //Navmeshを再開
            _owner.Agent.isStopped = false;
        }

        public override void OnUpdate()
        {
            if (_owner.Destination == null) return;

            float distance = Vector3.Distance(_owner.transform.position, _owner.Destination.position);
            if (distance > stopDistance)
            {
                _owner.ChangeState(FriendState.Chase);
            }
        }
    }
}