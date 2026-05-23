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
    }
}