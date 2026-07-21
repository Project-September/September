using InGame.Player;
using September.Common;
using UnityEngine;

namespace InGame.Bot
{
    public class AttackState : IBotState
    {
        private PlayerManager _target;
        private Vector3 _targetPos;
        private float _findRouteInterval = 0.5f;
        private float _findIntervalTimer;

        public void OnEnter(BotStateMachine stateMachine)
        {
            _target = BotDataBase.Instance.GetNearbyPlayer(stateMachine.gameObject);
            _findIntervalTimer = 0f;
        }

        public void OnExit(BotStateMachine stateMachine)
        {

        }

        public void OnUpdate(BotStateMachine stateMachine)
        {
            //プレイヤーが気絶したら終了
            if (_target == null || _target.IsStun)
            {
                stateMachine.ChangeState();
                return;
            }

            //毎フレームルート検索すると重いので一定間隔で行う
            _findIntervalTimer -= Time.deltaTime;

            if (_findIntervalTimer < 0)
            {
                _targetPos = _target.transform.position;
                _findIntervalTimer = _findRouteInterval;
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