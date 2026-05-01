using September.Common;
using UnityEngine;

namespace InGame.Bot
{
    public class BotStateMachine : MonoBehaviour
    {
        [field:SerializeField] public float _stopDistance { get; private set; }
        private IBotState _currentState;
        private RandomMoveState _randomState = new();

        public Vector3 _direction;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            ChangeState(_randomState);
        }
        // Update is called once per frame
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
            input.MoveDirection = _direction.normalized;

            return input;
        }
    }
}
