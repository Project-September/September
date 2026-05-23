using September.Common;

namespace September.InGame.Common.Stats
{
    public class AttackPowerBuildRuntime : IBuild<int>
    {
        int _conditionValue;
        int _maxConditionValue;
        int _currentCondition;
        int _advantageousValue;

        public int CurrentBuild => _advantageousValue * (_currentCondition / _conditionValue);

        public AttackPowerBuildRuntime(AttackPowerBuild build)
        {
            _conditionValue = build.ConditionValue;
            _maxConditionValue = build.MaxConditionValue;
            _currentCondition = build.DefaultConditionValue;
            _advantageousValue = build.AdvantageousValue;
        }

        public void UpdateBuild(int value)
        {
            if (_currentCondition >= _maxConditionValue) return;
            _currentCondition += value;
        }
    }
}
