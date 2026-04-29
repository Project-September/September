using UnityEngine;

namespace September
{
    public interface IBotState
    {
        public void OnEnter(BotStateMachine stateMachine);
        public void OnUpdate(BotStateMachine stateMachine);
        public void OnExit(BotStateMachine stateMachine);
    }
}
