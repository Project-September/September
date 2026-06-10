using September.Common;
using System;

namespace September.InGame.Common.Stats
{
    public abstract class BuildRuntimeBase<TCondition, TValue> : IBuild<TCondition, TValue> where TCondition : IComparable where TValue : IComparable
    {
        protected TCondition _conditionValue;
        protected TCondition _maxBuildCount;
        protected TCondition _currentCondition;
        protected TValue _advantageousValue;

        public abstract TValue CurrentBuild { get; }

        public abstract void UpdateBuild(TCondition value);
    }
}
