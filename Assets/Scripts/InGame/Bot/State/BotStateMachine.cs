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
        [SerializeField] private BotStateData _botStateData;
        [SerializeField] private PlayerMovement _playerMovement;
        /// <summary>NodeとBotが離れすぎて次のステートにする距離 </summary>
        [SerializeField] private float _stateChangeDistance;
        [SerializeField] private float _stopAmount;
        [SerializeField] private float _stopTime;
        [SerializeField] private Rigidbody _rigidbody;

        [field: SerializeField] public float StopDistance { get; private set; } = 1f;
        [field: SerializeField] public float GoalDistance { get; private set; } = 0.5f;
        [field: SerializeField] public float InteractDistance { get; private set; } = 2.5f;
        public NavigationController Navigation { get; private set; } = new();

        private IBotState _currentState;
        private Dictionary<StateType, IBotState> _stateDic = new();
        private float _stopTimer;

        void Start()
        {
            Navigation.StopDistance = StopDistance;
            Navigation.GoalDistance = GoalDistance;
            Navigation.CanVault = true;
            _stopTimer = _stopTime;

            ChangeState();
        }

        void Update()
        {
            //一定時間止まったら別のステートに変える
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

            //次のノードとPositionが離れすぎたら別のステートにする
            if (Navigation.NextNodePos.HasValue)
            {
                float dis = (Navigation.NextNodePos.Value - this.transform.position).sqrMagnitude;
                if (dis > _stateChangeDistance * _stateChangeDistance)
                {
                    ChangeState();
                }
            }

            _currentState?.OnUpdate(this);
        }

        public bool GetButton(PlayerButtons button)
        {
            if (!GameInput.I.IsActionInput) return false;

            switch (button)
            {
                case PlayerButtons.Jump:
                case PlayerButtons.Interact:
                case PlayerButtons.Attack:
                    return _currentState?.GetInputButton(this, button) ?? false;
                case PlayerButtons.Dash:
                    return true;
                default: return false;
            }
        }

        public Vector3 GetMoveDirection()
        {
            if (!GameInput.I.IsMoveInput) return Vector3.zero;
            Vector2 inputDirection = _currentState?.GetInputDirection(this) ?? Vector2.zero;
            return _playerMovement.IsGroundNet ? inputDirection.normalized : Vector2.zero;
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
                case StateType.RandomMove:
                    stateScriptType = typeof(RandomMoveState);
                    break;
                case StateType.Interact:
                    stateScriptType = typeof(InteractState);
                    break;
                case StateType.Attack:
                    stateScriptType = typeof(AttackState);
                    break;
                case StateType.None:
                default:
                    _currentState = null;
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
            return _botStateData.States[^1].Type;
        }
    }
}
