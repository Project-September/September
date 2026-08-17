using System;
using TMPro;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerEvasion
    {
        private EvasionData _evasionData;
        private Vector3 _rollDirection;
        private float _rollDistance;
        private float _startTime;
        private float _rollEndTime;
        private float _turnEndTime;
        private float _previousDistance;
        private Vector3 _previousDirection;
        private bool _isEvasion = false;

        public bool IsEvasion => _isEvasion;

        public event Action EndEvent;
        public PlayerEvasion(EvasionData evasionData)
        {
            if (evasionData == null)
            {
                Debug.LogError("");
            }
            _evasionData = evasionData;
        }

        public void OnEvasionStart(Vector2 inputDirection, Vector3 targetForward, float runnerTime,int JewelryCount)
        {
            if (_isEvasion)
                return;

            //クールダウン中
            if (_rollEndTime + _evasionData.Cooldown > runnerTime)
                return;

            _isEvasion = true;

            //入力がないならプレイヤーの方向にする
            if (inputDirection.magnitude < Mathf.Epsilon)
                inputDirection = targetForward;

            //回避方向を決める
            var targetDirection = new Vector2(targetForward.x, targetForward.z);
            var dot = Vector2.SignedAngle(inputDirection, targetDirection);
            var clampedDot = Mathf.Clamp(dot, -_evasionData.InputAngle, _evasionData.InputAngle);
            _rollDirection = Quaternion.Euler(0, clampedDot, 0) * targetForward;

            float weightCoefficient = GetWeightCoefficient(JewelryCount);

            _startTime = runnerTime;
            _rollEndTime = runnerTime + _evasionData.RollDuration * weightCoefficient;
            _rollDistance = _evasionData.RollDistance * weightCoefficient;
            var turnAngelT = Mathf.InverseLerp(0, _evasionData.InputAngle, Mathf.Abs(clampedDot));
            _turnEndTime = runnerTime + _evasionData.MaxTurnDuration * turnAngelT * weightCoefficient;
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

            float currentDistance = _rollDistance * speedT;
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

            float t = Mathf.InverseLerp(_startTime, _turnEndTime, runnerTime);

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

        public bool IsInvincible(float runnerTime)
        {
            return runnerTime - _startTime <= _evasionData.InvincibleTime;
        }

        private bool EndCheck(float runnerTime)
        {
            bool isEnd = runnerTime >= _rollEndTime;

            if (isEnd)
            {
                _isEvasion = false;
                EndEvent?.Invoke();
            }

            return isEnd;
        }

        /// <summary>
        /// 重み係数計算
        /// </summary>
        private float GetWeightCoefficient(int JewelryCount)
        {
            return 1f - (JewelryCount * _evasionData.WeightDecay);
        }
    }
}