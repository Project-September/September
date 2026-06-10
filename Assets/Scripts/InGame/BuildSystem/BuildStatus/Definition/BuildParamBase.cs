using System;
using UnityEngine;

namespace September.Common
{
    public abstract class BuildParamBase<TCondition, TValue> : BuildDataBase where TCondition : IComparable where TValue : IComparable
    {
        [Header("強化要素を得るための条件となる数値\n何回ごとに、何mごとになど")]
        [SerializeField, Min(0)] TCondition _conditionValue;
        [Header("初期の条件量")]
        [SerializeField] TCondition _defaultCondition;
        [Header("ビルドできる最大回数")]
        [SerializeField, Min(0)] TCondition _maxBuildCount;
        [Header("条件を満たしたときに得られる数値")]
        [SerializeField, Min(0)] TValue _advantageousValue;

        public TCondition ConditionValue => _conditionValue;
        public TCondition DefaultConditionValue => _defaultCondition;
        public TCondition MaxBuildCount => _maxBuildCount;
        public TValue AdvantageousValue => _advantageousValue;
    }
}
