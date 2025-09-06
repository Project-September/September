using System;
using UnityEngine;
using UnityEditor;
using StatType = InGame.Common.EffectableStatus.StatType;

namespace InGame.Common
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Player/StatusEffect")]
    public class StatusEffect : ScriptableObject
    {
        [SerializeField] private DurationType _durationType;
        [SerializeField, Min(0)] private float _duration;
        [SerializeField] private bool _isPeriodic;
        [SerializeField, Min(0)] private float _period;
        [SerializeField] private StatModifier[] _modifiers;
        [SerializeField] private StackOperation _stackOp;
        
        // Setting Parameter
        public DurationType DurationType => _durationType;
        /// <summary> 適用期間 </summary>
        public float Duration => Mathf.Max(_duration, 0);
        public bool IsPeriodic => _isPeriodic;
        /// <summary> Effect適用周期<br/>value ＜= 0 : not periodic </summary>
        public float Period => Mathf.Max(_period, 0.01f);
        public StatModifier[] ParamModifiers => _modifiers;
        public StackOperation StackOp => _stackOp;
        
        #if UNITY_EDITOR
        [CustomEditor(typeof(StatusEffect))]
        public class StatusEffectEditor : UnityEditor.Editor
        {
            private StatusEffect _statusEffect;
            private SerializedProperty _durationTypeProp;
            private SerializedProperty _durationProp;
            private SerializedProperty _isPeriodicProp;
            private SerializedProperty _periodProp;
            private SerializedProperty _modifiersProp;
            private SerializedProperty _stackOpProp;
            
            private void OnEnable()
            {
                _statusEffect = (StatusEffect)target;
                _durationTypeProp = serializedObject.FindProperty(nameof(_durationType));
                _durationProp = serializedObject.FindProperty(nameof(_duration));
                _isPeriodicProp = serializedObject.FindProperty(nameof(_isPeriodic));
                _periodProp = serializedObject.FindProperty(nameof(_period));
                _modifiersProp = serializedObject.FindProperty(nameof(_modifiers));
                _stackOpProp = serializedObject.FindProperty(nameof(_stackOp));
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                
                EditorGUILayout.PropertyField(_durationTypeProp);
                if (_statusEffect.DurationType != DurationType.Instant)
                {
                    if (_statusEffect.DurationType == DurationType.HasDuration) EditorGUILayout.PropertyField(_durationProp);
                    EditorGUILayout.PropertyField(_isPeriodicProp);
                    if (_statusEffect._isPeriodic) EditorGUILayout.PropertyField(_periodProp);
                }
                EditorGUILayout.Space(20);
                EditorGUILayout.PropertyField(_modifiersProp);
                EditorGUILayout.Space(20);
                EditorGUILayout.PropertyField(_stackOpProp);
                
                serializedObject.ApplyModifiedProperties();
            }
        }
        #endif
    }

    [Serializable]
    public struct StatModifier
    {
        [SerializeField] StatType _statType;
        [SerializeField] ModifierOperation _modifierOp;
        [SerializeField] MagnitudeCalculationType _magnitudeCalcType;
        [SerializeField] float _magnitude;
        [SerializeReference, SubclassSelector] CustomMagnitudeCalculation _customCalc;
            
        /// <summary> どの値に変更を加えるか </summary>
        public StatType StatType => _statType;
        /// <summary> 計算方法 </summary>
        public ModifierOperation ModifierOp => _modifierOp;
        public MagnitudeCalculationType MagnitudeCalcType => _magnitudeCalcType;
        /// <summary> 変更量 </summary>
        public float Magnitude => _magnitude;
        public CustomMagnitudeCalculation CustomCalcClass => _customCalc;
        
        #if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(StatModifier))]
        public class StatModifierDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);

                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                SerializedProperty statTypeProp = property.FindPropertyRelative(nameof(_statType));
                SerializedProperty modifierOpProp = property.FindPropertyRelative(nameof(_modifierOp));
                SerializedProperty magnitudeCalcTypeProp = property.FindPropertyRelative(nameof(_magnitudeCalcType));
                SerializedProperty magnitudeProp = property.FindPropertyRelative(nameof(_magnitude));
                SerializedProperty customCalcProp = property.FindPropertyRelative(nameof(_customCalc));

                EditorGUI.PropertyField(GetRect(position, 0), statTypeProp);
                EditorGUI.PropertyField(GetRect(position, 1), modifierOpProp);
                EditorGUI.PropertyField(GetRect(position, 2), magnitudeCalcTypeProp);

                if ((MagnitudeCalculationType)magnitudeCalcTypeProp.enumValueIndex == MagnitudeCalculationType.Constant)
                {
                    EditorGUI.PropertyField(GetRect(position, 3), magnitudeProp);
                }
                else if ((MagnitudeCalculationType)magnitudeCalcTypeProp.enumValueIndex == MagnitudeCalculationType.CustomClass)
                {
                    EditorGUI.PropertyField(GetRect(position, 3), customCalcProp);
                }

                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
            }

            Rect GetRect(Rect position, int count)
            {
                return new Rect(position.x, position.y + count * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), position.width, EditorGUIUtility.singleLineHeight);
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                SerializedProperty calcTypeProp = property.FindPropertyRelative(nameof(_magnitudeCalcType));
                int lines = 3;
                
                if (calcTypeProp.enumValueIndex != 1)
                {
                    lines++;
                }

                return EditorGUIUtility.singleLineHeight * lines + 2 * (lines - 1);
            }
        }
        #endif
    }

    [Serializable]
    public struct StackOperation
    {
        [SerializeField] private bool _canStack;
        [SerializeField, Min(2)] private int _limitCount;
        [SerializeField] private bool _refreshDuration;
        [SerializeField] private ExpirationPolicyType _expirationPolicy;
        [SerializeField] private bool _modifierAppliesPerStack;
        
        public bool CanStack => _canStack;
        /// <summary> 最大スタック数 </summary>
        public int LimitCount => Mathf.Max(_limitCount, 1);
        /// <summary> 複数スタック時に Duration をリセットするか </summary>
        public bool RefreshDuration => _refreshDuration;
        /// <summary> 終了時のスタックの減り方 </summary>
        public ExpirationPolicyType ExpirationPolicy => _expirationPolicy;
        /// <summary> スタック数に応じて効果量を増やすか </summary>
        public bool ModifierAppliesPerStack => _modifierAppliesPerStack;

        public override string ToString()
        {
            return $"LimitCount:{LimitCount}, RefreshDuration:{RefreshDuration}, ExpirationPolicy:{ExpirationPolicy}, ModifierAppliesPerStack:{ModifierAppliesPerStack}";
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(StackOperation))]
        public class StackOperationDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);
                
                var statusEffect = property.serializedObject.targetObject as StatusEffect;

                if (!statusEffect || statusEffect.DurationType == DurationType.Instant)
                {
                    EditorGUI.EndProperty();
                    return;
                }

                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                EditorGUI.LabelField(GetRect(position, 0), "StackOperation", EditorStyles.boldLabel);

                SerializedProperty canStackProp = property.FindPropertyRelative(nameof(_canStack));
                SerializedProperty limitCountProp = property.FindPropertyRelative(nameof(_limitCount));
                SerializedProperty refreshDurationProp = property.FindPropertyRelative(nameof(_refreshDuration));
                SerializedProperty expirationProp = property.FindPropertyRelative(nameof(_expirationPolicy));
                SerializedProperty modifierAppliesPerStackProp = property.FindPropertyRelative(nameof(_modifierAppliesPerStack));

                EditorGUI.PropertyField(GetRect(position, 1), canStackProp);

                if (statusEffect.StackOp.CanStack)
                {
                    EditorGUI.PropertyField(GetRect(position, 2), limitCountProp);
                    int count = 3;

                    if (statusEffect.DurationType == DurationType.HasDuration)
                    {
                        EditorGUI.PropertyField(GetRect(position, count), refreshDurationProp);
                        count++;
                    }
                
                    EditorGUI.PropertyField(GetRect(position, count), expirationProp);
                    count++;
                    EditorGUI.PropertyField(GetRect(position, count), modifierAppliesPerStackProp);
                }

                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
            }

            Rect GetRect(Rect position, int count)
            {
                return new Rect(position.x, position.y + count * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), position.width, EditorGUIUtility.singleLineHeight);
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                var statusEffect = property.serializedObject.targetObject as StatusEffect;

                if (!statusEffect || statusEffect.DurationType == DurationType.Instant) return 0;

                if (statusEffect.StackOp.CanStack)
                {
                    return EditorGUIUtility.singleLineHeight * 2 + 2;
                }
                
                int lines = 5;
                
                if (statusEffect.DurationType == DurationType.HasDuration)
                {
                    lines++;
                }
            
                return EditorGUIUtility.singleLineHeight * lines + 2 * (lines - 1);
            }
        }
        #endif
    }

    public enum DurationType
    {
        Instant,
        HasDuration,
        Infinite
    }
            
    /// <summary> 計算方法 </summary>
    public enum ModifierOperation
    {
        Add,
        Multiply,
        Override
    }

    public enum MagnitudeCalculationType
    {
        Constant,
        SetByCaller,
        CustomClass
    }

    public abstract class CustomMagnitudeCalculation
    {
        public abstract float CalculateMagnitude(EffectableStatus.ModifierSpec spec);
    }

    /// <summary> スタック削除時の設定 </summary>
    public enum ExpirationPolicyType
    {
        /// <summary> 全てのスタック削除 </summary>
        RemoveAllStack,
        /// <summary> スタック1つだけ削除 </summary>
        RemoveSingleStack,
        /// <summary> スタック1つ削除と効果時間 Refresh </summary>
        AndRefreshDuration
    }
}
