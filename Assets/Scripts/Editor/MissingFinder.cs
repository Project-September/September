using UnityEngine;
using UnityEditor;

public class MissingFinder
{
    [MenuItem("Tools/Find Missing Components In Prefabs")]
    static void FindMissingInPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int missingCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            // Prefab 内の全 Transform をチェック
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
            {
                Component[] comps = t.gameObject.GetComponents<Component>();
                foreach (var comp in comps)
                {
                    if (comp == null)
                    {
                        Debug.LogWarning(
                            $"Missing Component in Prefab: {path} → {GetPath(t.gameObject)}", 
                            prefab
                        );
                        missingCount++;
                    }
                }
            }
        }

        Debug.Log($"[Search Done] Missing Components in Prefabs: {missingCount}");
    }

    [MenuItem("Tools/Find Missing Components In Scene")]
    static void FindMissing()
    {
        int missingCount = 0;

        foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
        {
            // アタッチされている全コンポーネントを取得
            Component[] components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    Debug.LogWarning($"Missing Component found in: {GetPath(go)}", go);
                    missingCount++;
                }
            }
        }

        Debug.Log($"Missing Components: {missingCount}");
    }

    // 階層パスを返す補助
    private static string GetPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}