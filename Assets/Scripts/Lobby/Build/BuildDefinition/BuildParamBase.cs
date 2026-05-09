using System;
using UnityEngine;

namespace September.Common
{
    public abstract class BuildParamBase<T> : BuildDataBase where T : IComparable
    {
        [Header("強化要素を得るための条件となる数値")]
        [SerializeField, Min(0)] T _conditionValue;
        [Header("条件を満たしたときに得られる数値")]
        [SerializeField, Min(0)] T _advantageousValue;
        [Header("強化の最大量")]
        [SerializeField, Min(0)] T _maxAdvantageousValue;

        public T ConditionValue => _conditionValue;
        public T AdvantageousValue => _advantageousValue;
        public T MaxAdvantageousValue => _maxAdvantageousValue;
    }
}
