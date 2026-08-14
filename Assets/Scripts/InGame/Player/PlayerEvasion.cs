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

        public void OnEvasionStart(Vector2 inputDirection, Vector3 targetForward, float runnerTime, Action endAction)
        {
            if (_isEvasion)
                return;

            _endAction = endAction;
            _isEvasion = true;

            //ì¸óÕÇ™Ç»Ç¢Ç»ÇÁÉvÉåÉCÉÑÅ[ÇÃï˚å¸Ç…Ç∑ÇÈ
            if (inputDirection.magnitude < Mathf.Epsilon)
                inputDirection = targetForward;

            //âÒîï˚å¸ÇåàÇﬂÇÈ
            var targetDirection = new Vector2(targetForward.x, targetForward.z);
            var dot = Vector2.Dot(inputDirection, targetDirection);
            var clampedDot = Mathf.Clamp(dot, -_evasionData.InputAngle, _evasionData.InputAngle);
            _rollDirection = Quaternion.Euler(0, clampedDot, 0) * targetForward;

            _startTime = runnerTime;
            _endTime = runnerTime + _evasionData.RollDuration;
            _previousDistance = 0f;

            Debug.Log(_startTime + ":" + _endTime);
        }

        public Vector3 Move(Vector3 targetPosition, float runnerTime)
        {
            if (!_isEvasion)
                return targetPosition;

            if (runnerTime >= _endTime)
            {
                _isEvasion = false;
                _endAction?.Invoke();
                return targetPosition;
            }

            float t = Mathf.InverseLerp(_startTime, _endTime, runnerTime);

            float speedT = _evasionData.RollSpeed.Evaluate(t);

            float currentDistance = _evasionData.RollDistance * speedT;
            float deltaDistance = currentDistance - _previousDistance;

            Debug.Log(
    $"[Evasion]\n" +
    $"runnerTime      : {runnerTime} | valid:{IsValid(runnerTime)}\n" +
    $"startTime       : {_startTime} | valid:{IsValid(_startTime)}\n" +
    $"endTime         : {_endTime} | valid:{IsValid(_endTime)}\n" +
    $"t               : {t} | valid:{IsValid(t)}\n" +
    $"speedT          : {speedT} | valid:{IsValid(speedT)}\n" +
    $"rollDistance    : {_evasionData.RollDistance} | valid:{IsValid(_evasionData.RollDistance)}\n" +
    $"currentDistance : {currentDistance} | valid:{IsValid(currentDistance)}\n" +
    $"previousDistance: {_previousDistance} | valid:{IsValid(_previousDistance)}\n" +
    $"deltaDistance   : {deltaDistance} | valid:{IsValid(deltaDistance)}\n" +
    $"rollDirection   : {_rollDirection} | valid:{IsValid(_rollDirection)}\n" +
    $"targetPosition  : {targetPosition} | valid:{IsValid(targetPosition)}"
);


            _previousDistance = currentDistance;

            return targetPosition + _rollDirection * deltaDistance;
        }

        public void IsIgnoreAttack()
        {

        }
        static bool IsValid(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        static bool IsValid(Vector3 value)
        {
            return IsValid(value.x) &&
                   IsValid(value.y) &&
                   IsValid(value.z);
        }
    }
}