using System;
using UnityEngine;

namespace InGame.Player
{
    public class PlayerEvasion
    {
        private EvasionData _evasionData;
        private Vector3 _moveDirection;
        private float _rollDistance;
        private float _startTime;
        private float _rollEndTime;
        private float _turnEndTime;
        private float _previousDistance;
        private Vector3 _startDirection;
        private bool _isEvading = false;

        public bool IsEvading => _isEvading;

        public event Action EvasionEnded;
        public PlayerEvasion(EvasionData evasionData)
        {
            if (evasionData == null)
            {
                Debug.LogError("[PlayerEvasion] EvasionData is null");
            }
            _evasionData = evasionData;
        }

        public bool TryStartEvasion(Vector2 inputDirection, Vector3 currentForward, float runnerTime, int playerWeight, out float rollAnimationDurationScale)
        {
            rollAnimationDurationScale = 1f;

            if (_isEvading)
                return false;

            //クールダウン中
            if (_rollEndTime + _evasionData.Cooldown > runnerTime)
                return false;

            _isEvading = true;

            //入力がないならプレイヤーの方向にする
            if (inputDirection.magnitude < Mathf.Epsilon)
                inputDirection = new Vector2(currentForward.x, currentForward.z);

            //回避方向を決める
            var targetDirection = new Vector2(currentForward.x, currentForward.z);
            var angle = Vector2.SignedAngle(inputDirection, targetDirection);
            var clampedAngle = Mathf.Clamp(angle, -_evasionData.InputAngle, _evasionData.InputAngle);
            _moveDirection = Quaternion.Euler(0, clampedAngle, 0) * currentForward;

            float weightCoefficient = CalculateWeightCoefficient(playerWeight);

            _startTime = runnerTime;
            _rollEndTime = runnerTime + _evasionData.RollDuration * weightCoefficient;
            _rollDistance = _evasionData.RollDistance * weightCoefficient;
            var turnProgress = Mathf.InverseLerp(0, _evasionData.InputAngle, Mathf.Abs(clampedAngle));
            _turnEndTime = runnerTime + _evasionData.MaxTurnDuration * turnProgress * weightCoefficient;
            _previousDistance = 0f;
            _startDirection = currentForward;

            rollAnimationDurationScale = _evasionData.RollDuration * weightCoefficient;

            return true;
        }

        public Vector3 Move(Vector3 targetPosition, float runnerTime)
        {
            if (!_isEvading)
                return targetPosition;

            if (EndCheck(runnerTime))
                return targetPosition;

            float t = Mathf.InverseLerp(_startTime, _rollEndTime, runnerTime);

            float speedProgress = _evasionData.RollSpeedCurve.Evaluate(t);

            float currentDistance = _rollDistance * speedProgress;
            float distanceDelta = currentDistance - _previousDistance;

            _previousDistance = currentDistance;

            return targetPosition + _moveDirection * distanceDelta;
        }

        public Vector3 Turn(Vector3 forward, float runnerTime)
        {
            if (!_isEvading)
                return forward;

            if (EndCheck(runnerTime))
                return forward;

            float t = Mathf.InverseLerp(_startTime, _turnEndTime, runnerTime);
            float speedT = _evasionData.TurnSpeedCurve.Evaluate(t);

            Vector3 currentDirection = Vector3.Slerp(_startDirection, _moveDirection, speedT);

            currentDirection.y = 0f;
            return currentDirection.normalized;
        }

        public bool IsInvincible(float runnerTime)
        {
            float evasionTime = runnerTime - _startTime;
            return evasionTime >= _evasionData.StartInvincibleTime && evasionTime <= _evasionData.InvincibleTime;
        }

        private bool EndCheck(float runnerTime)
        {
            bool hasEnded = runnerTime >= _rollEndTime;

            if (hasEnded)
            {
                _isEvading = false;
                EvasionEnded?.Invoke();
            }

            return hasEnded;
        }

        /// <summary>
        /// 重み係数計算
        /// </summary>
        private float CalculateWeightCoefficient(int JewelryCount)
        {
            return 1f - (JewelryCount * _evasionData.WeightDecay);
        }
    }
}
