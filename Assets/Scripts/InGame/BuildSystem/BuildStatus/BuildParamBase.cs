using System;
using UnityEngine;

namespace September.Common
{
    public abstract class BuildParamBase<TCondition, TValue> : BuildDataBase where TCondition : IComparable where TValue : IComparable
    {
        [Header("強化要素を得るための条件となる数値\n何回ごとに、何mごとになど")]
        [SerializeField, Min(0)] TCondition _conditionValue;
        [Header("条件の最大量\n何回まで、何mまでなど")]
        [SerializeField, Min(0)] TCondition _maxConditionValue;
        [Header("初期の条件量")]
        [SerializeField] TCondition _defaultCondition;
        [Header("条件を満たしたときに得られる数値")]
        [SerializeField, Min(0)] TValue _advantageousValue;

        public TCondition ConditionValue => _conditionValue;
        public TCondition MaxConditionValue => _maxConditionValue;
        public TCondition DefaultConditionValue => _defaultCondition;
        public TValue AdvantageousValue => _advantageousValue;
    }
}
