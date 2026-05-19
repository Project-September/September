using Fusion;
using September;
using UnityEngine;

namespace InGame.Bot
{
    public class AttackState : IBotState
    {
        //パラメータ系はいつか外でいじれるようにしたい
        private NetworkObject _target;
        private Vector3 _targetPos;
        private float _findRouteInterval = 0.5f;
        private float _timer;
        public void OnEnter(BotStateMachine stateMachine)
        {
            _target = BotDataBase.Instance.GetNearbyPlayer(stateMachine.NetObject);
        }

        public void OnExit(BotStateMachine stateMachine)
        {
            stateMachine.InputIsAttack = false;
        }

        public void OnUpdate(BotStateMachine stateMachine)
        {
            if (_target == null)
            {
                stateMachine.ChangeState();
                return;
            }

            _timer -= Time.deltaTime;

            if (_timer < 0)
            {
                Debug.Log(_target.gameObject.name);
                _targetPos = _target.transform.position;
                _timer = _findRouteInterval;
            }

            stateMachine.InputIsAttack = stateMachine.Navigation.IsComplete;

            stateMachine.Navigation.GetDestinationInput(stateMachine.transform.position, _targetPos);
        }
    }
}
