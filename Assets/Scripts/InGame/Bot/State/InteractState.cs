using InGame.Interact;
using September;
using September.Common;
using UnityEngine;

namespace InGame.Bot
{
    public class InteractState : IBotState
    {
        private InteractableBase _targetInteractable;

        public void OnEnter(BotStateMachine stateMachine)
        {
            _targetInteractable = BotDataBase.Instance.GetNearByInteract(stateMachine.gameObject);
        }

        public void OnExit(BotStateMachine stateMachine)
        {

        }

        public void OnUpdate(BotStateMachine stateMachine)
        {
            if(_targetInteractable == null)
            {
                OnEnter(stateMachine);
                return;
            }

            stateMachine.Navigation.GetDestinationInput(stateMachine.transform.position, _targetInteractable.transform.position);

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
            if (stateMachine.Navigation.IsComplete)
            {
             
                var targetSqr = (_targetInteractable.transform.position - stateMachine.transform.position).sqrMagnitude;
                if (targetSqr > stateMachine.InteractDistance * stateMachine.InteractDistance)
                {
                    Vector3 targetPos = _targetInteractable.transform.position;
                    targetPos.y = stateMachine.transform.position.y;
                    return targetPos - stateMachine.transform.position;
                }
            }
            return stateMachine.Navigation.InputDirection;
        }
    }
}
