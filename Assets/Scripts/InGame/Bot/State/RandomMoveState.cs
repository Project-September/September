using System;
using System.Collections.Generic;
using System.Threading;
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
            stateMachine.Navigation.GetDestinationInput(_currentPos, stateMachine.transform.position);
            if (stateMachine.Navigation.IsComplete)
            {
                Debug.Log("NextNode");
                OnEnter(stateMachine);
            }
        }
    }
}