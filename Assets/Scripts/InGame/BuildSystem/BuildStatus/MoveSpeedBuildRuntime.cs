using September.Common;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    public class MoveSpeedBuildRuntime : BuildRuntimeBase<float, float>
    {
        public override float CurrentBuild => _advantageousValue * (int)Mathf.Min((_currentCondition / _conditionValue), _maxBuildCount); // 最大ビルド回数を超えないように調整

        public MoveSpeedBuildRuntime(MoveSpeedBuild build)
        {
            _conditionValue = build.ConditionValue;
            _currentCondition = build.DefaultConditionValue;
            _maxBuildCount = build.MaxBuildCount;
            _advantageousValue = build.AdvantageousValue;
        }

        public override void UpdateBuild(float value)
        {
            if (_currentCondition / _conditionValue >= _maxBuildCount) return;
            _currentCondition += value;
        }
    }
}
