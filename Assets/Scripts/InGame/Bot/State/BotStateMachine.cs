using System;
using System.Collections.Generic;
using Fusion;
using InGame.Player;
using September.Common;
using UnityEngine;
using Random = UnityEngine.Random;

namespace InGame.Bot
{
    public class BotStateMachine : MonoBehaviour
    {
        [SerializeField] private NetworkObject _netObject;
        public NetworkObject NetObject => _netObject;
        [SerializeField] private BotStateData _botStateData;
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private float _stopAmount;
        [SerializeField] private float _stopTime;
        [SerializeField] private Rigidbody _rigidbody;
        [field: SerializeField] public float StopDistance { get; private set; }
        private IBotState _currentState;
        private Dictionary<StateType, IBotState> _stateDic = new();

        private float _stopTimer;
        private Vector3 _inputDirection;
        private bool _inputIsVault;
        public bool InputIsAttack;//一旦Public　あとでちゃんと書く
        public NavigationController Navigation = new();

        void Start()
        {
            Navigation.StopDistance = StopDistance;
            Navigation.CanVault = true;

            ChangeState();
        }

        void Update()
        {
            if (_rigidbody.linearVelocity.magnitude < _stopAmount && GameInput.I.IsMoveInput && _playerMovement.IsGroundNet)
            {
                _stopTimer -= Time.deltaTime;
                if (_stopTimer < 0)
                {
                    _stopTimer = _stopTime;
                    ChangeState();
                }
            }
            else
            {
                _stopTimer = _stopTime;
            }


            _currentState?.OnUpdate(this);

            _inputIsVault = Navigation.IsVaultInput;
            _inputDirection = Navigation.InputDirection;

        }

        public PlayerInput GetInput()
        {
            PlayerInput input = new();
            input.Buttons.Set(PlayerButtons.Jump, _inputIsVault);
            input.Buttons.Set(PlayerButtons.Dash, false);
            return input;
        }

        public bool GetButton(PlayerButtons button)
        {
            if (!GameInput.I.IsActionInput) return false;
            switch (button)
            {
                case PlayerButtons.Jump:
                    return _inputIsVault;
                case PlayerButtons.Dash:
                    return true;
                case PlayerButtons.Attack:
                    return InputIsAttack;
                default: return false;
            }
        }

        public Vector3 GetMoveDirection()
        {
            if (!GameInput.I.IsMoveInput) return Vector3.zero;
            return _playerMovement.IsGroundNet ? _inputDirection.normalized : Vector2.zero;
        }

        public void OnDrawGizmos()
        {
            Navigation.ShowGizmo();
        }

        public void ChangeState()
        {
            _currentState?.OnExit(this);

            Type stateScriptType = null;
            StateType stateType = GetNextState();
            switch (stateType)
            {
                case StateType.None:
                    return;
                case StateType.RandomMove:
                    stateScriptType = typeof(RandomMoveState);
                    break;
                case StateType.Interact:
                    stateScriptType = typeof(InteractState);
                    break;
                case StateType.Attack:
                    stateScriptType = typeof(AttackState);
                    break;
                default:
                    return;
            }

            if (!typeof(IBotState).IsAssignableFrom(stateScriptType))
            {
                Debug.LogError($"{stateScriptType.Name} は IBotState を実装していません");
                return;
            }

            if (!_stateDic.TryGetValue(stateType, out var state))
            {
                state = (IBotState)Activator.CreateInstance(stateScriptType);
                _stateDic.Add(stateType, state);
            }

            _currentState = state;
            _currentState?.OnEnter(this);
        }

        private StateType GetNextState()
        {
            if (_botStateData == null) return StateType.None;

            int random = Random.Range(0, _botStateData.SumProbability);

            int sum = 0;
            foreach (var state in _botStateData.States)
            {
                sum += state.Probability;
                if (random < sum)
                {
                    return state.Type;
                }
            }
            return _botStateData.States[_botStateData.States.Length - 1].Type;
        }
    }
}

