#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace September.InGame.Player.Data
{
    public static class SerializedObjectExtensions
    {
        public static void SetArrayManagedReferences<T>(this SerializedProperty dest, IReadOnlyList<T> from)
        {
            if (!dest.isArray) throw new ArgumentException("dest in not an array");
            dest.arraySize = from.Count;
            for (int i = 0; i < from.Count; i++)
            {
                dest.GetArrayElementAtIndex(i).managedReferenceValue = from[i];
            }
        }

        public static void SetArrayBoxedValues<T>(this SerializedProperty dest, IReadOnlyList<T> from)
        {
            if (!dest.isArray) throw new ArgumentException("dest in not an array");
            Debug.Log("setarray: " + dest.arrayElementType);

            dest.arraySize = from.Count;
            for (int i = 0; i < from.Count; i++)
            {
                dest.GetArrayElementAtIndex(i).boxedValue = from[i];
            }
        }

        public static void SetArrayObjectReferenceValues<T>(this SerializedProperty dest, IReadOnlyList<T> from) where T : UnityEngine.Object
        {
            if (!dest.isArray) throw new ArgumentException("dest in not an array");
            Debug.Log("setarray: " + dest.arrayElementType);

            dest.arraySize = from.Count;
            for (int i = 0; i < from.Count; i++)
            {
                dest.GetArrayElementAtIndex(i).objectReferenceValue = from[i];
            }
        }

        public static Component PasteComponentAsNew(Component source, GameObject destination)
        {
            Debug.Log("Copy: " + UnityEditorInternal.ComponentUtility.CopyComponent(source));

            var comp = destination.AddComponent(source.GetType());
            Debug.Log("Paste: " + UnityEditorInternal.ComponentUtility.PasteComponentValues(comp));

            return comp;
        }

        public static void ReplaceReferencesGlobalToLocal(Component dst, Component src)
        {
            SerializedObject dstSo = new(dst);
            SerializedProperty dstProp = dstSo.GetIterator();

            while (dstProp.NextVisible(true))
            {
                if (dstProp.propertyType != SerializedPropertyType.ObjectReference) continue;

                if (dstProp.objectReferenceValue is Component dstComponent)
                {
                    Debug.Log($"{dstProp.objectReferenceValue} {dstComponent.gameObject} {src.gameObject}");

                    if (dstComponent.gameObject == src.gameObject || dstComponent.gameObject.transform.IsChildOf(src.gameObject.transform.root))
                    {
                        dstProp.objectReferenceValue = dst.GetComponentInChildren(dstComponent.GetType());
                    }
                }
                else if (dstProp.objectReferenceValue is GameObject gameObject
                         && gameObject == src.gameObject)
                {
                    dstProp.objectReferenceValue = null;
                }
            }

            dstSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
