using InGame.Player;
using September.Common;
using UnityEngine;

namespace InGame.Bot
{
    public class BotManager : PlayerManager
    {
        [SerializeField] private BotStateMachine _stateMachine;
        protected override void LateUpdate()
        {

        }
        public override bool GetPlayerInput(out PlayerInput input)
        {
            input = _stateMachine.GetInput();
            return true;
        }
    }
}
