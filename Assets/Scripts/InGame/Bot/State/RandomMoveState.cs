using September.Common;
using UnityEngine;

namespace InGame.Bot
{
    /// <summary>
    /// テスト用ステート
    /// </summary>
    public class RandomMoveState : IBotState
    {
        private Vector3 _currentPos;
        public void OnEnter(BotStateMachine stateMachine)
        {
            _currentPos = NodeProvider.Instance.GetRandomNode().Position;
        }

        public void OnExit(BotStateMachine stateMachine)
        {

        }

        public void OnUpdate(BotStateMachine stateMachine)
        {
            stateMachine.Navigation.GetDestinationInput(stateMachine.transform.position, _currentPos);
            if (stateMachine.Navigation.IsComplete)
            {
                stateMachine.ChangeState();
            }
        }

        public bool? GetInputButton(BotStateMachine stateMachine, PlayerButtons button)
        {
            switch (button)
            {
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