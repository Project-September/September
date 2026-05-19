using System;
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
            input = new();
            input.MoveDirection = _stateMachine.GetMoveDirection();
            foreach (PlayerButtons button in Enum.GetValues(typeof(PlayerButtons)))
            {
                input.Buttons.Set(button, _stateMachine.GetButton(button));
            }
            return true;
        }
    }
}
