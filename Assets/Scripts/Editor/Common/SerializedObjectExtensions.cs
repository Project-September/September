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
    }
}
#endif