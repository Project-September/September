using InGame.Interact;
using September;

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

            stateMachine.InputIsInteract = true;

            if (_targetInteractable.IsInCooldown())
            {
                stateMachine.ChangeState();
            }
        }
    }
}
