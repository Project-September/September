using September.Common;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    public class FastInteractBuildRuntime : BuildRuntimeBase<int, float>
    {
        public override float CurrentBuild => _advantageousValue * Mathf.Min((_currentCondition / _conditionValue), _maxBuildCount); // 最大ビルド回数を超えないように調整

        public FastInteractBuildRuntime(FastInteractBuild build)
        {
            _conditionValue = build.ConditionValue;
            _maxBuildCount = build.MaxBuildCount;
            _currentCondition = build.DefaultConditionValue;
            _advantageousValue = build.AdvantageousValue;
        }

        public override void UpdateBuild(int value)
        {
            if (_currentCondition / _conditionValue >= _maxBuildCount) return;
            _currentCondition += value;
        }
    }
}
