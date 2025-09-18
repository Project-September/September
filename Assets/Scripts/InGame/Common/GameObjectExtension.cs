using UnityEngine;

public static class GameObjectExtension
{
    public static T GetComponentInHierarchy<T>(this GameObject gameObject) where T : Component
    {
        // 自身をチェック
        T component = gameObject.GetComponent<T>();
        if (component != null) return component;

        // 親をチェック
        component = gameObject.GetComponentInParent<T>();
        if (component != null) return component;

        // 子をチェック
        component = gameObject.GetComponentInChildren<T>();
        return component;
    }
}
