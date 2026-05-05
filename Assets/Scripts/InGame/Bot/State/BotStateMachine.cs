using September.Common;
using UnityEngine;

namespace InGame.Bot
{
    public class BotStateMachine : MonoBehaviour
    {
        [SerializeField] private float _stopAmount;
        [SerializeField] private float _stopTime;
        [SerializeField] private Rigidbody _rigidbody;
        [field:SerializeField] public float _stopDistance { get; private set; }
        private IBotState _currentState;
        private RandomMoveState _randomState = new();

        public Vector3 _direction;
        public bool _vault;
        private float _stopTimer;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            ChangeState(_randomState);
        }
        // Update is called once per frame
        void Update()
        {   
            _currentState?.OnUpdate(this);

            if(_rigidbody.linearVelocity.magnitude < _stopAmount)
            {
                _stopTimer -= Time.deltaTime;
                if(_stopTimer < 0 )
                {
                    Debug.Log("ƒtƒŠ[ƒY");
                    //_currentState?.OnEnter(this);
                }
            }
            else
            {
                _stopTimer = _stopTime;
            }
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
            input.Buttons.Set(PlayerButtons.Jump, _vault);
            input.Buttons.Set(PlayerButtons.Dash, false);
            return input;
        }

        public void OnDrawGizmos()
        {
            if (_randomState == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_randomState._nodes[_randomState._nodes.Count - 1].Position, 1f);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_randomState._nodes[_randomState._index].Position,1f);
        }
    }
}
