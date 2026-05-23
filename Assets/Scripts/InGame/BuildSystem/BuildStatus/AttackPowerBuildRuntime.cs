using September.Common;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    public class AttackPowerBuildRuntime : BuildRuntimeBase<int, int>
    {
        public override int CurrentBuild => _advantageousValue * Mathf.Min((_currentCondition / _conditionValue), _maxBuildCount); // 最大ビルド回数を超えないように調整

        public AttackPowerBuildRuntime(AttackPowerBuild build)
        {
            _conditionValue = build.ConditionValue;
            _currentCondition = build.DefaultConditionValue;
            _maxBuildCount = build.MaxBuildCount;
            _advantageousValue = build.AdvantageousValue;
        }

        public override void UpdateBuild(int value)
        {
            if (_currentCondition / _conditionValue >= _maxBuildCount) return;
            _currentCondition += value;
        }
    }
}
