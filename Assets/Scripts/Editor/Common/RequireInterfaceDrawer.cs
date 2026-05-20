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
        private readonly GUIStyle _style = new(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            normal =
            {
                textColor = Color.gray
            }
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (RequireInterfaceAttribute)attribute;

            EditorGUI.BeginProperty(position, label, property);

            label.tooltip = $"Requires component implementing {attr.InterfaceType.Name}";

            // ドラッグ中の表示制御
            if (position.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    bool isValid = false;
                    foreach (Object obj in DragAndDrop.objectReferences)
                    {
                        if (FindValidComponent(obj, attr.InterfaceType) != null)
                        {
                            isValid = true;
                            break;
                        }
                    }

                    if (!isValid)
                    {
                        // マウスポインタを変更してアサイン不可能であることを明示する
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

                        // DragUpdated/Perform のイベントを消費して内部処理を阻止
                        Event.current.Use();
                    }
                }
            }

            Object current = property.objectReferenceValue;

            Object picked = EditorGUI.ObjectField(
                position,
                label,
                current,
                typeof(Object),
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

                GUI.Label(
                    infoRect,
                    attr.InterfaceType.Name,
                    _style
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

                foreach (Component c in components)
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
