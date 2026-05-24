using InGame.Player;
using September;
using September.Common;
using UnityEngine;

namespace InGame.Bot
{
    public class AttackState : IBotState
    {
        //パラメータ系はいつか外でいじれるようにしたい
        private PlayerManager _target;
        private Vector3 _targetPos;
        private float _findRouteInterval = 0.5f;
        private float _timer;
        public void OnEnter(BotStateMachine stateMachine)
        {
            _target = BotDataBase.Instance.GetNearbyPlayer(stateMachine.gameObject);
        }

        public void OnExit(BotStateMachine stateMachine)
        {

        }

        public void OnUpdate(BotStateMachine stateMachine)
        {
            if (_target == null)
            {
                stateMachine.ChangeState();
                return;
            }

            if (_target.IsStun)
            {
                _target = null;
                OnEnter(stateMachine);
                return;
            }

            _timer -= Time.deltaTime;

            if (_timer < 0)
            {
                _targetPos = _target.transform.position;
                _timer = _findRouteInterval;
            }

            stateMachine.Navigation.GetDestinationInput(stateMachine.transform.position, _targetPos);
        }

        public bool? GetInputButton(BotStateMachine stateMachine, PlayerButtons button)
        {
            switch (button)
            {
                case PlayerButtons.Attack:
                    return stateMachine.Navigation.IsComplete;
                case PlayerButtons.Jump:
                    return stateMachine.Navigation.IsVaultInput;
            }

            return null;
        }

        public Vector2 GetInputDirection(BotStateMachine stateMachine)
        {
            return stateMachine.Navigation.InputDirection;
        }
    }
}