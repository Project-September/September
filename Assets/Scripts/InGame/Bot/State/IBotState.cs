using September.Common;
using UnityEngine;

namespace InGame.Bot
{
    public interface IBotState
    {
        public void OnEnter(BotStateMachine stateMachine);
        public void OnUpdate(BotStateMachine stateMachine);
        public void OnExit(BotStateMachine stateMachine);
        public bool? GetInputButton(BotStateMachine stateMachine, PlayerButtons button);
        public Vector2 GetInputDirection(BotStateMachine stateMachine);
    }
}