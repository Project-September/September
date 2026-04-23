using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

namespace InGame.Common
{
    public class EffectableStatus : NetworkBehaviour
    {
        private OldStat _dummy;
        protected readonly List<ActiveStatusEffect> ActiveEffectSpecs = new();

        void Update()
        {
            UpdateEffects(Time.deltaTime);
        }

        void UpdateEffects(float deltaTime)
        {
            for (int i = ActiveEffectSpecs.Count - 1; i >= 0; i--)
            {
                // 期限切れのEffectは削除
                if (ActiveEffectSpecs[i].IsExpired)
                {
                    ActiveEffectSpecs.RemoveAt(i);
                    continue;
                }

                ActiveEffectSpecs[i].Tick(deltaTime);
            }
        }

        public void AddEffect(StatusEffect effect)
        {
            if (!StatusEffectsContainer.TryGetInstance(out var container)) return;

            RPC_AddEffect(container.GetStatusEffectIndex(effect));
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

        public void ApplyModifier(ModifierSpec modifier)
        {
            if (!TryGetStatFromType(modifier.StatType, out var stat)) return;

            stat.ApplyModifier(modifier.ModifierOp, modifier.Magnitude);
        }

        public void CalculationValue(StatType type)
        {
            if (!TryGetStatFromType(type, out var stat)) return;

            if (stat.IsValid)
            {
                stat.CalculationValue();
            }
        }

        public void AddEffect(StatusEffectSpec effectSpec)
        {
            Debug.Log($"AddEffect: {effectSpec}");
            var index = ActiveEffectSpecs.FindIndex(statusEffectSpec => statusEffectSpec.Spec.DefEffect == effectSpec.DefEffect);

            if (index == -1)
            {
                _ = new ActiveStatusEffect(effectSpec, this);
            }
            else
            {
                ActiveEffectSpecs[index].AddStack();
            }
        }

        public void RemoveEffect(StatusEffect effect)
        {
            if (!StatusEffectsContainer.TryGetInstance(out var container)) return;

            RPC_RemoveEffect(container.GetStatusEffectIndex(effect));
        }

        public bool TryGetReadOnlyStat(StatType type,out IReadOnlyStat readStat)
        {
            readStat = null;

            if (!TryGetStatFromType(type, out var stat)) return false;

            readStat = stat;
            return true;
        }

        // Tick同期にしたほうがよさそう
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_RemoveEffect(int statusIndex)
        {
            if (!StatusEffectsContainer.TryGetInstance(out var container)) return;

            var effect = container.GetStatusEffect(statusIndex);

            var targetIndex = ActiveEffectSpecs.FindIndex(activeEffect => activeEffect.Spec.DefEffect == effect);

            if (targetIndex != -1)
            {
                ActiveEffectSpecs[targetIndex].RemoveStack();
            }
        }

        void PostApplyEffect(ActiveStatusEffect activeEffect)
        {
            OnPostApplyEffect(activeEffect);

            foreach (var modifier in activeEffect.Spec.Modifiers)
            {
                OnNextValue(modifier.StatType);
            }
        }

        protected virtual void OnNextValue(StatType type)
        {
            if (!TryGetStatFromType(type, out var stat)) return;

            if (stat.IsValid)
            {
                stat.OnPostApplyEffect.OnNext(stat.Value);
            }
        }
            
        protected virtual void OnPostApplyEffect(ActiveStatusEffect activeEffect) { }

        protected virtual bool TryGetStatFromType(StatType type, out OldStat stat)
        {
            stat = _dummy;
            Debug.LogError("stat is null");
            return false;
        }



        /// <summary> StatEffectを発動中管理する </summary>
        public class ActiveStatusEffect
        {
            private readonly EffectableStatus _targetStatus;
            private readonly List<float> _timers;
            private readonly List<float> _periodTimers;

            public readonly StatusEffectSpec Spec;
            public int StackCount { get; private set; }
            public float LeadTimer => _timers.Any() ? _timers[0] : 0;
            public bool IsExpired => StackCount <= 0;

            public ActiveStatusEffect(StatusEffectSpec spec, EffectableStatus targetStatus)
            {
                _targetStatus = targetStatus;
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
                    ApplyEffect();
                    return;
                }

                // 後にListを使った更新通知を出すのでこのタイミングでAddする
                _targetStatus.ActiveEffectSpecs.Add(this);

                AddStack();
            }

            public void AddStack()
            {
                // Stack が増えない条件
                if (Spec.DurationType == DurationType.Instant || (!Spec.StackOp.CanStack && StackCount > 0) || Spec.StackOp.LimitCount <= StackCount) return;

                StackCount++;
                ApplyEffect();

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
                            ApplyEffect();
                        }
                    }
                }
            }

            void ApplyEffect()
            {
                foreach (var modifier in Spec.Modifiers)
                {
                    _targetStatus.ApplyModifier(modifier);
                }

                _targetStatus.PostApplyEffect(this);
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

                // stat に変更の通知
                foreach (var modifier in Spec.Modifiers)
                {
                    _targetStatus.CalculationValue(modifier.StatType);
                }
            }
        }
    }

    /// <summary> Effect 用共通 </summary>
    public enum StatType
    {
        Health,
        Speed,
        Stamina,
        StaminaRegen,
        StaminaConsumption,
        AttackDamage
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
        public float Magnitude => MagnitudeCalcType == MagnitudeCalculationType.CustomClass ? CustomClass.CalculateMagnitude(this) : _magnitude;

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
            if (MagnitudeCalcType == MagnitudeCalculationType.SetByCaller || MagnitudeCalcType == MagnitudeCalculationType.CustomClass)
            {
                _magnitude = mag;
            }
        }

        public override string ToString()
        {
            return $"StatType:{StatType}, ModifierOp:{ModifierOp}, MagnitudeCalcType:{MagnitudeCalcType}, Magnitude:{Magnitude}";
        }
    }
}
