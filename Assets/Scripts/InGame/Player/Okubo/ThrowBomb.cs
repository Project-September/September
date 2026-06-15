using Fusion;
using September.Common;
using UnityEngine;

namespace InGame.Player.Okubo
{
    public class ThrowBomb : NetworkBehaviour
    {
        [SerializeField] private PlayerInputManager _playerInputManager;
        [SerializeField] private NetworkObject _bombPrefab;
        [SerializeField] private float _throwInterval;
        [SerializeField] private int _throwCount;
        [SerializeField] private float _throwRange;
        [SerializeField, Range(0, 0.5f)] private float _randomDirectionAmount = 0.4f;
        [SerializeField] private int _candidateCount;
        private float _coolTimer;

        private void Start()
        {
            if (_playerInputManager == null)
                _playerInputManager = GetComponent<PlayerInputManager>();
        }

        public override void FixedUpdateNetwork()
        {
            if (!_playerInputManager.GetPlayerInput(out var input)) return;

            if (input.Buttons.IsSet(PlayerButtons.Ability1))
            {
                OnThrow();
            }

            if (_coolTimer > 0)
                _coolTimer -= Runner.DeltaTime;
        }

        private void OnThrow()
        {
            if (_coolTimer > 0) return;

            _coolTimer = _throwInterval;
            if (_throwCount <= 0) return;

            //‚¢‚¢Š´‚¶‚É‚Î‚ç‚¯‚é‚æ‚¤‚É‚·‚é
            var directions = new Vector3[_throwCount];

            float step = 360f / _throwCount;

            for (int i = 0; i < _throwCount; i++)
            {
                // ’S“–‚·‚éŠp“x
                float angle = i * step;

                // ­‚µƒ‰ƒ“ƒ_ƒ€‚É‚¸‚ç‚·i}40%j
                angle += Random.Range(-step * _randomDirectionAmount, step * _randomDirectionAmount);

                float rad = angle * Mathf.Deg2Rad;

                directions[i] = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
            }

            //¶¬E“Š‚°‚éˆ—
        }
    }
}
