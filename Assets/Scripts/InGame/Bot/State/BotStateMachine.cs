

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

        private float _stopTimer;
        private Vector3 InputDirection;
        private bool InputIsVault;
        public NavigationController Navigation = new();

        void Start()
        {
            ChangeState(_randomState);
            Navigation.StopDistance = StopDistance;
            Navigation.CanVault = true;
        }

        void Update()
        {
            if (_rigidbody.linearVelocity.magnitude < _stopAmount && GameInput.I.IsMoveInput && _playerMovement.IsGroundNet)
            {
                _stopTimer -= Time.deltaTime;
                if (_stopTimer < 0)
                {
                    _stopTimer = _stopTime;
                    _currentState?.OnEnter(this);
                }
            }
            else
            {
                _stopTimer = _stopTime;
            }


            _currentState?.OnUpdate(this);

            InputIsVault = Navigation.IsVaultInput;
            InputDirection = Navigation.InputDirection;

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
        public void OnDrawGizmos()
        {
            Navigation.ShowGizumo();
        }
    }
}

