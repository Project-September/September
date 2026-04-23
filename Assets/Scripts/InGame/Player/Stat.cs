using Fusion;
using InGame.Player;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using static InGame.Common.EffectableStatus;

namespace InGame.Common
{

    public interface IReadOnlyStat
    {
        public StatType Type { get; }
        public ParameterData BaseParameter { get; }
        public bool IsValid { get; }
        public float BaseValue { get; }
        public float Value { get; }
    }
    public struct NetworkStat : INetworkStruct
    {
        public float BaseValue;
        public float Value;
    }
    public class Stat : IReadOnlyStat
    {
        public ParameterData BaseParameter { get; private set; }
        public StatType Type { get; private set; }
        public bool IsValid { get; private set; }
        //メインの値
        public float Value { get; private set; }
        //バフ前の値
        public float BaseValue { get; private set; }

        private readonly ReactiveProperty<int> _currentBaseValue = new(0);
        private readonly ReactiveProperty<float> _currentValue = new(0);
        //OnChangedで読んでメインの値をこっちにしたい（Value => ReactiveCurrentBaseValue.Valueみたいな感じ）
        public ReadOnlyReactiveProperty<int> ReactiveCurrentBaseValue => _currentBaseValue.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<float> ReactiveCurrentValue => _currentValue.ToReadOnlyReactiveProperty();

        private readonly List<ActiveStatusEffect> _activeEffects;
        public readonly Subject<float> OnValueChanged;
        public readonly Subject<float> OnPostApplyEffect;
        public Stat(ParameterData parameterData, List<ActiveStatusEffect> activeEffects)
        {
            IsValid = true;
            _activeEffects = activeEffects;
            Value = parameterData.DefaultValue;
            BaseValue = parameterData.DefaultValue;
            OnValueChanged = new Subject<float>();
            OnPostApplyEffect = new Subject<float>();
            BaseParameter = parameterData;

        }
        //public Stat(List<ActiveStatusEffect> activeEffects, ParameterData parameter)
        //{
        //    IsValid = true;
        //    _activeEffects = activeEffects;
        //    Value = parameter.DefaultValue;
        //    BaseValue = parameter.DefaultValue;
        //    OnValueChanged = new Subject<float>();
        //    OnPostApplyEffect = new Subject<float>();
        //    BaseParameter = parameter;
        //}

        /// <summary> エフェクトの更新通知 Effectの変更をValueに反映させる </summary>
        public void CalculationValue()
        {
            Value = BaseValue;
            float multiplyValue = 1;

            foreach (var effect in _activeEffects)
            {
                if (effect.Spec.DurationType == DurationType.Instant || effect.Spec.IsPeriodic) continue;

                for (int i = 0; i < (effect.Spec.StackOp.ModifierAppliesPerStack ? effect.StackCount : 1); i++)
                {
                    foreach (var modifier in effect.Spec.Modifiers)
                    {
                        if (modifier.ModifierOp == ModifierOperation.Add) Value += modifier.Magnitude;
                        else if (modifier.ModifierOp == ModifierOperation.Multiply) multiplyValue *= modifier.Magnitude;
                        else
                        {
                            Value = modifier.Magnitude;
                            return;
                        }
                    }
                }
            }

            Value *= multiplyValue;
            OnValueChanged.OnNext(Value);
        }
        public void AddBaseValue(float amount)
        {
            SetBaseValue(BaseValue + amount);
        }

        public void SetBaseValue(float value)
        {
            BaseValue = Mathf.Clamp(value, BaseParameter.MinValue, BaseParameter.MaxValue);
            CalculationValue();
        }
        public void AddValue(float amount)
        {
            SetValue(Value + amount);
        }
        public void SetValue(float value)
        {
            Value = Mathf.Clamp(value, BaseParameter.MinValue, BaseParameter.MaxValue);
            OnValueChanged.OnNext(Value);
        }


        public void ApplyModifier(ModifierOperation operation, float magnitude)
        {
            switch (operation)
            {
                case ModifierOperation.Add:
                    BaseValue += magnitude;
                    break;
                case ModifierOperation.Multiply:
                    BaseValue *= magnitude;
                    break;
                case ModifierOperation.Override:
                    BaseValue = magnitude;
                    break;
            }

            CalculationValue();
        }

    }
}