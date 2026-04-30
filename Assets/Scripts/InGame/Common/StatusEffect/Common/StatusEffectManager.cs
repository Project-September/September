using System.Collections.Generic;
using System.Linq;
using Fusion;
using InGame.Common;
using UnityEngine;

namespace September.InGame.Common.Stats
{
    /// <summary>
    /// 状態異常を管理するクラス
    /// </summary>
    public class StatusEffectManager : StatsEffectorBase
    {
        private readonly List<ActiveStatusEffect> _activeEffectSpecs = new();

        private void Update()
        {
            UpdateEffects(Time.deltaTime);
        }
        
        public override StatsContainer Apply(StatsContainer stats)
        {
            foreach (var (type, stat) in stats.Stats)
            {
                var newStat = ApplyStat(stat);
                stats.Stats.Set(type, newStat);
            }

            return stats;
            
            Stat ApplyStat(Stat stat)
            {
                float addValue = 0;
                float multiplyValue = 1;

                foreach (var effect in _activeEffectSpecs)
                {
                    if (effect.Spec.DurationType == DurationType.Instant || effect.Spec.IsPeriodic) continue;

                    for (int i = 0; i < (effect.Spec.StackOp.ModifierAppliesPerStack ? effect.StackCount : 1); i++)
                    {
                        foreach (var modifier in effect.Spec.Modifiers)
                        {
                            if (modifier.StatType != stat.StatType) continue;
                            
                            switch (modifier.ModifierOp)
                            {
                                case ModifierOperation.Add:
                                    addValue += modifier.Magnitude;
                                    break;
                                case ModifierOperation.Multiply:
                                    multiplyValue *= modifier.Magnitude;
                                    break;
                                case ModifierOperation.Override:
                                    stat.SetValue(modifier.Magnitude);
                                    return stat;
                                    break;
                            }
                        }
                    }
                }

                stat.SetValue(stat.Value + addValue);
                stat.SetValue(stat.Value * multiplyValue);
                
                return stat;
            }
        }

        private void UpdateEffects(float deltaTime)
        {
            for (int i = _activeEffectSpecs.Count - 1; i >= 0; i--)
            {
                // 期限切れのEffectは削除
                if (_activeEffectSpecs[i].IsExpired)
                {
                    _activeEffectSpecs.RemoveAt(i);
                    continue;
                }

                _activeEffectSpecs[i].Tick(deltaTime);
            }
        }

        public void AddEffect(StatusEffect effect)
        {
            if (!StatusEffectsContainer.TryGetInstance(out var container)) return;

            RPC_AddEffect(container.GetStatusEffectIndex(effect));
        }

        public void AddEffect(StatusEffectSpec effectSpec)
        {
            Debug.Log($"AddEffect: {effectSpec}");
            var index = _activeEffectSpecs.FindIndex(statusEffectSpec =>
                statusEffectSpec.Spec.DefEffect == effectSpec.DefEffect);

            if (index == -1)
            {
                _activeEffectSpecs.Add(new ActiveStatusEffect(effectSpec));
            }
            else
            {
                _activeEffectSpecs[index].AddStack();
            }
        }

        public void RemoveEffect(StatusEffect effect)
        {
            if (!StatusEffectsContainer.TryGetInstance(out var container)) return;

            RPC_RemoveEffect(container.GetStatusEffectIndex(effect));
        }

        // Tick同期にしたほうがよさそう
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_AddEffect(int statusIndex)
        {
            if (!StatusEffectsContainer.TryGetInstance(out var container)) return;

            container.GetStatusEffect(statusIndex);
            var status = container.GetStatusEffect(statusIndex);
            AddEffect(new StatusEffectSpec(status));
        }

        // Tick同期にしたほうがよさそう
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_RemoveEffect(int statusIndex)
        {
            if (!StatusEffectsContainer.TryGetInstance(out var container)) return;

            var effect = container.GetStatusEffect(statusIndex);

            var targetIndex = _activeEffectSpecs.FindIndex(activeEffect => activeEffect.Spec.DefEffect == effect);

            if (targetIndex != -1)
            {
                _activeEffectSpecs[targetIndex].RemoveStack();
            }
        }

        /// <summary> StatEffectを発動中管理する </summary>
        protected class ActiveStatusEffect
        {
            private readonly List<float> _timers;
            private readonly List<float> _periodTimers;

            public readonly StatusEffectSpec Spec;
            public int StackCount { get; private set; }
            public float LeadTimer => _timers.Any() ? _timers[0] : 0;
            public bool IsExpired => StackCount <= 0;

            public ActiveStatusEffect(StatusEffectSpec spec)
            {
                _timers = new List<float>();
                _periodTimers = new List<float>();
                Spec = spec;
                StackCount = 0;

                InitEnableEffectSpec();
            }

            private void InitEnableEffectSpec()
            {
                if (Spec.DurationType == DurationType.Instant)
                {
                    return;
                }

                AddStack();
            }

            public void AddStack()
            {
                // Stack が増えない条件
                if (Spec.DurationType == DurationType.Instant || (!Spec.StackOp.CanStack && StackCount > 0) ||
                    Spec.StackOp.LimitCount <= StackCount) return;

                StackCount++;

                if (Spec.DurationType == DurationType.HasDuration)
                {
                    _timers.Add(Spec.Duration);

                    if (Spec.StackOp.RefreshDuration)
                    {
                        for (int i = 0; i < StackCount; i++)
                        {
                            _timers[i] = Spec.Duration;
                        }
                    }
                }

                // ModifierAppliesPerStack == false なら一つのTimerでいい
                if (Spec.IsPeriodic && (Spec.StackOp.ModifierAppliesPerStack || !_periodTimers.Any()))
                {
                    _periodTimers.Add(Spec.Period);
                }
            }

            public void Tick(float deltaTime)
            {
                if (Spec.DurationType == DurationType.Instant) return;

                // has duration Timer 管理
                if (Spec.DurationType == DurationType.HasDuration)
                {
                    for (int i = 0; i < StackCount; i++)
                    {
                        _timers[i] -= deltaTime;
                    }

                    if (_timers.Any() && _timers[0] <= 0)
                    {
                        RemoveStack();
                    }
                }

                // period timer
                if (Spec.IsPeriodic)
                {
                    for (int i = 0; i < (Spec.StackOp.ModifierAppliesPerStack ? StackCount : 1); i++)
                    {
                        _periodTimers[i] -= deltaTime;

                        if (_periodTimers[i] <= 0)
                        {
                            _periodTimers[i] += Spec.Period;
                        }
                    }
                }
            }

            public void RemoveStack()
            {
                if (StackCount <= 0 || Spec.DurationType == DurationType.Instant) return;

                if (Spec.StackOp.ExpirationPolicy == ExpirationPolicyType.RemoveAllStack)
                {
                    _timers.Clear();
                    _periodTimers.Clear();
                    StackCount = 0;
                }
                else
                {
                    if (Spec.IsPeriodic) _periodTimers.RemoveAt(0);

                    if (Spec.DurationType == DurationType.HasDuration)
                    {
                        _timers.RemoveAt(0);

                        if (Spec.StackOp.ExpirationPolicy == ExpirationPolicyType.AndRefreshDuration)
                        {
                            for (int i = 0; i < StackCount; i++)
                            {
                                _timers[i] = Spec.Duration;
                            }
                        }
                    }

                    StackCount--;
                }
            }
        }

        public struct StatusEffectSpec
        {
            public readonly StatusEffect DefEffect;
            public readonly DurationType DurationType;
            public float Duration;
            public readonly bool IsPeriodic;
            public readonly float Period;
            public readonly ModifierSpec[] Modifiers;
            public readonly StackOperation StackOp;

            public StatusEffectSpec(StatusEffect statusEffect)
            {
                DefEffect = statusEffect;
                DurationType = statusEffect.DurationType;
                Duration = statusEffect.Duration;
                IsPeriodic = statusEffect.IsPeriodic;
                Period = statusEffect.Period;
                Modifiers = new ModifierSpec[statusEffect.ParamModifiers.Length];
                StackOp = statusEffect.StackOp;

                for (int i = 0; i < Modifiers.Length; i++)
                {
                    Modifiers[i] = new ModifierSpec(statusEffect.ParamModifiers[i]);
                }
            }

            public override string ToString()
            {
                string s =
                    $"Name:{DefEffect.name}\nDurationType:{DurationType}, Duration:{Duration}, IsPeriodic:{IsPeriodic}, Period:{Period}\nModifiers\n";

                foreach (var modifier in Modifiers)
                {
                    s += modifier + "\n";
                }

                return s + "StackOp\n" + StackOp;
            }
        }

        public struct ModifierSpec
        {
            private float _magnitude;

            public readonly StatType StatType;
            public readonly ModifierOperation ModifierOp;
            public readonly MagnitudeCalculationType MagnitudeCalcType;
            public readonly CustomMagnitudeCalculation CustomClass;

            public float Magnitude => MagnitudeCalcType == MagnitudeCalculationType.CustomClass
                ? CustomClass.CalculateMagnitude(this)
                : _magnitude;

            public ModifierSpec(StatModifier statModifier)
            {
                StatType = statModifier.StatType;
                ModifierOp = statModifier.ModifierOp;
                MagnitudeCalcType = statModifier.MagnitudeCalcType;
                _magnitude = statModifier.Magnitude;
                CustomClass = statModifier.CustomCalcClass;
            }

            public void SetByCallerMagnitude(float mag)
            {
                if (MagnitudeCalcType == MagnitudeCalculationType.SetByCaller ||
                    MagnitudeCalcType == MagnitudeCalculationType.CustomClass)
                {
                    _magnitude = mag;
                }
            }

            public override string ToString()
            {
                return
                    $"StatType:{StatType}, ModifierOp:{ModifierOp}, MagnitudeCalcType:{MagnitudeCalcType}, Magnitude:{Magnitude}";
            }
        }
    }
}