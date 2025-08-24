#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class HierarchyPathCopier
{
    [MenuItem("GameObject/コピー/階層パスをコピー", false, 0)]
    public static void CopyHierarchyPath()
    {
        if (Selection.activeGameObject != null)
        {
            string path = GetFullPath(Selection.activeGameObject.transform);
            EditorGUIUtility.systemCopyBuffer = path;
            Debug.Log("コピーしたパス: " + path);
        }
    }

    private static string GetFullPath(Transform transform)
    {
        return transform.parent == null ? transform.name : GetFullPath(transform.parent) + "/" + transform.name;
    }

    [MenuItem("GameObject/コピー/階層パスをコピー", true)]
    public static bool ValidateCopyHierarchyPath()
    {
        return Selection.activeGameObject != null;
    }
}
#endif