#if UNITY_EDITOR
using Common.Attribute;
using UnityEditor;
using UnityEngine;

namespace September.Editor.Common
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute), true)]
    public sealed class ReadOnlyCopyableDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            var got = TryGetAssetFromProperty(property, out var obj, out var assetPath, out var guid);
            
            // アセット参照だったら飛べる
            if (got)
            {
                bool isAsset = !string.IsNullOrEmpty(assetPath);
                var e = Event.current;
                
                if (position.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        EditorGUIUtility.PingObject(obj);

                        if (isAsset && e.clickCount == 2)
                        {
                            AssetDatabase.OpenAsset(obj);
                        }

                        e.Use();
                    }
                }
            }
        }
        
        static bool TryGetAssetFromProperty(SerializedProperty prop, out Object obj, out string path, out string guid)
        {
            obj = null; path = null; guid = null;

            // ObjectReference
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                obj = prop.objectReferenceValue;
                if (!obj) return false;
                path = AssetDatabase.GetAssetPath(obj);
                guid = string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
                return true;
            }

            // AssetReference
            var guidProp = prop.FindPropertyRelative("m_AssetGUID");
            if (guidProp == null) return false;
            guid = guidProp.stringValue;
            if (string.IsNullOrEmpty(guid)) return false;

            path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return false;

            var subName = prop.FindPropertyRelative("m_SubObjectName")?.stringValue;
            if (!string.IsNullOrEmpty(subName))
            {
                foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                {
                    if (sub && sub.name == subName) { obj = sub; break; }
                }
            }
            if (!obj) obj = AssetDatabase.LoadMainAssetAtPath(path);

            return obj;
        }
    }
}
#endif