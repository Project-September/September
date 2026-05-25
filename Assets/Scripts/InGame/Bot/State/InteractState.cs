using InGame.Interact;
using September;
using September.Common;
using UnityEngine;

namespace InGame.Bot
{
    public class InteractState : IBotState
    {
        private InteractableBase _targetInteractable;
        private Vector3 _targetPosition;

        public void OnEnter(BotStateMachine stateMachine)
        {
            _targetInteractable = BotDataBase.Instance.GetNearByInteract(stateMachine.gameObject);
        }

        public void OnExit(BotStateMachine stateMachine)
        {

        }

        public void OnUpdate(BotStateMachine stateMachine)
        {
            if (_targetInteractable == null)
            {
                OnEnter(stateMachine);
                return;
            }

            Vector3 targetPosition = _targetInteractable.GetInteractPosition();
            stateMachine.Navigation.GetDestinationInput(stateMachine.transform.position, targetPosition);

            if (_targetInteractable.IsInCooldown())
            {
                stateMachine.ChangeState();
            }
        }

        public bool? GetInputButton(BotStateMachine stateMachine, PlayerButtons button)
        {
            switch (button)
            {
                case PlayerButtons.Interact:
                    return true;
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
