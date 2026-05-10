#if UNITY_EDITOR

using System;
using September.Common.Attribute;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace September.Editor.Common
{
    [CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
    public class RequireInterfaceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (RequireInterfaceAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);
            
            label.tooltip = $"Requires component implementing {attr.InterfaceType.Name}";

            Object current = property.objectReferenceValue;

            Object picked = EditorGUI.ObjectField(
                position,
                label,
                current,
                typeof(UnityEngine.Object),
                true
            );

            if (picked == null)
            {
                property.objectReferenceValue = null;
            }
            else
            {
                Component validComponent = FindValidComponent(
                    picked,
                    attr.InterfaceType
                );

                if (validComponent != null)
                {
                    property.objectReferenceValue = validComponent;
                }
            }

            // 未アサインならInterface名表示
            if (property.objectReferenceValue == null)
            {
                Rect infoRect = position;
                infoRect.xMin += EditorGUIUtility.labelWidth + 4;
                infoRect.xMax -= 20;

                GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
                style.alignment = TextAnchor.MiddleRight;
                style.normal.textColor = Color.gray;

                GUI.Label(
                    infoRect,
                    attr.InterfaceType.Name,
                    style
                );
            }

            EditorGUI.EndProperty();
        }

        private Component FindValidComponent(Object obj, Type interfaceType)
        {
            // Component直接
            if (obj is Component component)
            {
                if (interfaceType.IsAssignableFrom(component.GetType()))
                {
                    return component;
                }
            }

            // GameObjectから探索
            if (obj is GameObject go)
            {
                var components = go.GetComponents<Component>();

                foreach (var c in components)
                {
                    if (c == null)
                        continue;

                    if (interfaceType.IsAssignableFrom(c.GetType()))
                    {
                        return c;
                    }
                }
            }

            return null;
        }
    }
}

#endif