#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace September.Editor.Common
{
    public static class ExComponentUtility
    {
        public static Component PasteComponentAsNew(Component source, GameObject destination)
        {
            Debug.Log("Copy: " + UnityEditorInternal.ComponentUtility.CopyComponent(source));

            // PasteComponentAsNewだとなぜか失敗するので、AddComponentしてからPasteComponentValuesする
            var comp = destination.AddComponent(source.GetType());
            Debug.Log("Paste: " + UnityEditorInternal.ComponentUtility.PasteComponentValues(comp));

            return comp;
        }

        /// <summary>
        /// 別プレハブのコンポーネントなどのグローバルな参照を、
        /// 自身のゲームオブジェクト内のコンポーネントなどのローカルな参照に置き換えます
        /// </summary>
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
