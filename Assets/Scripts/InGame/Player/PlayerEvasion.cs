using System;
using UnityEngine;

namespace InGame.Player
{
    [System.Serializable]
    public class PlayerEvasion
    {
        private EvasionData _evasionData;
        private Action _endAction;
        private Vector3 _rollDirection;
        private float _startTime;
        private float _endTime;
        private float _timer;
        private float _previousDistance;
        private bool _isEvasion = false;

        public bool IsEvasion => _isEvasion;

        public PlayerEvasion(EvasionData evasionData)
        {
            if (evasionData == null)
            {
                Debug.LogError("");
            }
            _evasionData = evasionData;
        }

        public void OnEvasionStart(Vector2 inputDirection, Vector3 targetForward, float deltaTime, Action endAction)
        {
            if (_isEvasion)
                return;

            _endAction = endAction;
            _isEvasion = true;

            //‰ñ”ð•ûŒü‚ðŒˆ‚ß‚é
            var targetDirection = new Vector2(targetForward.x, targetForward.z);
            var dot = Vector2.Dot(inputDirection, targetDirection);
            //TODO: InputDirection == v2.zero‚ÌŽž‚Év2.one‚É‚·‚é
            var clampedDot = Mathf.Clamp(dot, -_evasionData.InputAngle, _evasionData.InputAngle);
            _rollDirection = Quaternion.Euler(0, clampedDot, 0) * targetForward;

            _startTime = deltaTime;
            _endTime = deltaTime + _evasionData.RollDuration;
            _previousDistance = 0f;
            _timer = 0f;
        }

        public Vector3 Move(Vector3 targetPosition, float deltaTime)
        {
            _timer += deltaTime;

            if (!_isEvasion)
                return targetPosition;

            if(_timer >= _endTime)
            {
                _isEvasion = false;
                _endAction?.Invoke();
                return targetPosition;
            }

            float t = Mathf.InverseLerp(_startTime, _endTime, _timer);

            float speedT = _evasionData.RollSpeed.Evaluate(t);

            float currentDistance = _evasionData.RollDistance * speedT;
            float deltaDistance = currentDistance - _previousDistance;

            _previousDistance = currentDistance;

            return targetPosition + _rollDirection * deltaDistance;
        }

        public void IsIgnoreAttack()
        {

        }
    }
}
