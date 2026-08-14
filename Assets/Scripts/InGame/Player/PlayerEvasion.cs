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

            //“ü—Í‚ª‚È‚¢‚È‚çƒvƒŒƒCƒ„[‚Ì•ûŒü‚É‚·‚é
            if (inputDirection.magnitude < Mathf.Epsilon)
                inputDirection = targetForward;

            //‰ñ”ğ•ûŒü‚ğŒˆ‚ß‚é
            var targetDirection = new Vector2(targetForward.x, targetForward.z);
            var dot = Vector2.SignedAngle(inputDirection, targetDirection);
            var clampedDot = Mathf.Clamp(dot, -_evasionData.InputAngle, _evasionData.InputAngle);
            _rollDirection = Quaternion.Euler(0, clampedDot, 0) * targetForward;

            Debug.Log(
    $"[Evasion Direction]\n" +
    $"inputDirection : {inputDirection}\n" +
    $"targetForward  : {targetForward}\n" +
    $"targetDirection: {targetDirection}\n" +
    $"dot            : {dot}\n" +
    $"InputAngle     : {_evasionData.InputAngle}\n" +
    $"clampedDot     : {clampedDot}\n" +
    $"rollDirection  : {_rollDirection}\n" +
    $"rollMagnitude  : {_rollDirection.magnitude}"
);

            _startTime = runnerTime;
            _endTime = runnerTime + _evasionData.RollDuration;
            _previousDistance = 0f;
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

            _previousDistance = currentDistance;

            return targetPosition + _rollDirection * deltaDistance;
        }
    }
}