using Fusion;
using September.Common;
using UnityEngine;

namespace InGame.Player.Okubo
{
    public class ThrowBomb : NetworkBehaviour
    {
        [SerializeField] private PlayerInputManager _playerInputManager;
        [SerializeField] private BombControl _bombPrefab;
        [SerializeField] private Transform _clonePosition;

        [SerializeField] private float _throwInterval = 1f;
        [SerializeField] private int _throwCount = 3;
        [SerializeField] private float _throwRange = 5f;

        [SerializeField, Range(0f, 0.5f)]
        private float _randomDirectionAmount = 0.4f;

        [SerializeField] private float _throwPower = 10f;
        [SerializeField] private float _throwUpAmount = 3f;

        private float _coolTimer;
        private NetworkButtons _previousButtons;

        private void Start()
        {
            if (_playerInputManager == null)
            {
                _playerInputManager = GetComponent<PlayerInputManager>();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (_coolTimer > 0f)
            {
                _coolTimer -= Runner.DeltaTime;
            }

            if (!_playerInputManager.GetPlayerInput(out var input))
                return;

            var pressed = input.Buttons.GetPressed(_previousButtons);

            if (pressed.IsSet(PlayerButtons.Ability1))
            {
                OnThrow();
            }

            _previousButtons = input.Buttons;
        }

        private void OnThrow()
        {
            if (_coolTimer > 0f)
                return;

            if (_throwCount <= 0)
                return;

            _coolTimer = _throwInterval;

            foreach (var direction in GenerateDirections())
            {
                Debug.Log(direction);
                var bomb = Runner.Spawn(_bombPrefab, _clonePosition.position + direction, Quaternion.identity);

                Vector3 force = direction * _throwPower + Vector3.up * _throwUpAmount;
                bomb.SetData(force, Object.InputAuthority);
            }
        }

        private Vector3[] GenerateDirections()
        {
            var directions = new Vector3[_throwCount];

            float step = 360f / _throwCount;

            for (int i = 0; i < _throwCount; i++)
            {
                float angle = i * step;

                angle += Random.Range(-step * _randomDirectionAmount, step * _randomDirectionAmount);

                float rad = angle * Mathf.Deg2Rad;

                directions[i] = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
            }

            return directions;
        }
    }
}