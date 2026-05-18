using System;
using System.Collections.Generic;
using InGame.Player;
using September.Common;
using UnityEngine;
using Random = UnityEngine.Random;

namespace InGame.Bot
{
    public class BotStateMachine : MonoBehaviour
    {
        [SerializeField] private BotStateData _botStateData;
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private float _stopAmount;
        [SerializeField] private float _stopTime;
        [SerializeField] private Rigidbody _rigidbody;
        [field: SerializeField] public float StopDistance { get; private set; }
        private IBotState _currentState;
        private Dictionary<StateType, IBotState> _stateDic = new();

        private float _stopTimer;
        private Vector3 InputDirection;
        private bool InputIsVault;
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

            InputIsVault = Navigation.IsVaultInput;
            InputDirection = Navigation.InputDirection;

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
            foreach (var state in _botStateData.states)
            {
                sum += state._probability;
                if (random < sum)
                {
                    return state._stateType;
                }
            }

            return _botStateData.states[_botStateData.states.Length - 1]._stateType;
        }
    }
}

