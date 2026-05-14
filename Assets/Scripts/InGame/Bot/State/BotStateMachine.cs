using InGame.Player;
using September.Common;
using UnityEngine;

namespace InGame.Bot
{
    public class BotStateMachine : MonoBehaviour
    {
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private float _stopAmount;
        [SerializeField] private float _stopTime;
        [SerializeField] private Rigidbody _rigidbody;
        [field: SerializeField] public float StopDistance { get; private set; }
        private IBotState _currentState;
        private RandomMoveState _randomState = new();

        public Vector3 InputDirection;
        public bool InputIsVault;

        void Start()
        {
            ChangeState(_randomState);
        }

        void Update()
        {
            _currentState?.OnUpdate(this);
        }

        public void ChangeState(IBotState state)
        {
            _currentState?.OnExit(this);
            _currentState = state;
            _currentState.OnEnter(this);
        }

        public PlayerInput GetInput()
        {
            PlayerInput input = new();
            input.Buttons.Set(PlayerButtons.Jump, InputIsVault);
            input.Buttons.Set(PlayerButtons.Dash, false);
            return input;
        }
        public bool GetButton(PlayerButtons button)
        {
            if (!GameInput.I.IsActionInput) return false;
            switch (button)
            {
                case PlayerButtons.Jump:
                    return InputIsVault;
                case PlayerButtons.Dash:
                    return true;
                default: return false;
            }
        }

        public Vector3 GetMoveDirection()
        {
            if (!GameInput.I.IsMoveInput) return Vector3.zero;
            return _playerMovement.IsGroundNet ? InputDirection.normalized : Vector2.zero;
        }
    }
}
