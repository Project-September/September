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
        private float _rollEndTime;
        private float _turnEndTime;
        private float _previousDistance;
        private Vector3 _previousDirection;
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

            //入力がないならプレイヤーの方向にする
            if (inputDirection.magnitude < Mathf.Epsilon)
                inputDirection = targetForward;

            //回避方向を決める
            var targetDirection = new Vector2(targetForward.x, targetForward.z);
            var dot = Vector2.SignedAngle(inputDirection, targetDirection);
            var clampedDot = Mathf.Clamp(dot, -_evasionData.InputAngle, _evasionData.InputAngle);
            _rollDirection = Quaternion.Euler(0, clampedDot, 0) * targetForward;

            _startTime = runnerTime;
            _rollEndTime = runnerTime + _evasionData.RollDuration;
            var turnAngelT = Mathf.InverseLerp(0, _evasionData.InputAngle, Mathf.Abs(clampedDot));
            _turnEndTime = runnerTime + _evasionData.MaxTurnDuration * turnAngelT;
            _previousDistance = 0f;
            _previousDirection = targetDirection;
        }

        public Vector3 Move(Vector3 targetPosition, float runnerTime)
        {
            if (!_isEvasion)
                return targetPosition;

            if (EndCheck(runnerTime))
                return targetPosition;

            float t = Mathf.InverseLerp(_startTime, _rollEndTime, runnerTime);

            float speedT = _evasionData.RollSpeedCurve.Evaluate(t);

            float currentDistance = _evasionData.RollDistance * speedT;
            float deltaDistance = currentDistance - _previousDistance;

            _previousDistance = currentDistance;

            return targetPosition + _rollDirection * deltaDistance;


        }

        public Vector3 Turn(Vector3 forward, float runnerTime)
        {
            if (!_isEvasion)
                return forward;

            if (EndCheck(runnerTime))
                return forward;

            float t = Mathf.InverseLerp(
                _startTime,
                _turnEndTime,
                runnerTime
            );
            Debug.Log(t);

            float speedT = _evasionData.TurnSpeedCurve.Evaluate(t);

            Vector3 currentDirection = Vector3.Slerp(_previousDirection, _rollDirection, speedT);

            // Yを固定
            currentDirection.y = 0f;

            Vector3 deltaDirection = currentDirection - _previousDirection;

            // 前回方向のYも0として保持
            _previousDirection = currentDirection;

            Vector3 result = forward + deltaDirection;
            result.y = forward.y;

            return result;
        }

        private bool EndCheck(float runnerTime)
        {
            bool isEnd = runnerTime >= _rollEndTime;
            if (isEnd)
            {
                _isEvasion = false;
                _endAction?.Invoke();
            }
            return isEnd;
        }
    }
}