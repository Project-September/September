#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class HitboxDebugUtility
{
    private static bool _debugMode = false;
    public static bool IsDebugModeEnabled => _debugMode;

#if UNITY_EDITOR
    [MenuItem("Debug/Toggle Hitbox Debug %h")]
    public static void ToggleHitboxDebug()
    {
        _debugMode = !_debugMode;
        Debug.Log($"[HitboxDebug] Debug Mode: {_debugMode}");
        SceneView.RepaintAll();
    }
#endif
    
    /// <summary>
    /// ワイヤーカプセルを描画する。デバッグモード中のみ描画される
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawWireCapsule(Vector3 start, Vector3 end, float radius, Color color, float duration = 0f)
    {
        if (IsDebugModeEnabled)
        {
            DebugDrawUtility.DrawWireCapsule(start, end, radius, color, duration);
        }
    }
    
    /// <summary>
    /// ワイヤー球を描画する。デバッグモード中のみ描画される
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawWireSphere(Vector3 position, float radius, Color color, float duration = 0f)
    {
        if (IsDebugModeEnabled)
        {
            DebugDrawUtility.DrawWireSphere(position, radius, color, duration);
        }
    }

    /// <summary>
    /// そのフレームだけ、単一のワイヤーフレームBoxを描画する
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawBoxOneFrame(
        Vector3 center,
        Vector3 halfExtents,
        Quaternion rot,
        Color color)
    {
        if (!IsDebugModeEnabled) return;
        // duration=0 で1フレームだけ表示
        DebugDrawUtility.DrawOrientedWireBox(center, halfExtents, rot, color, 0f);
    }

    /// <summary>
    /// コライダー形状を描画する。デバッグモード中のみ描画される
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void DrawCollider(Collider collider, Color color, float duration = 0f)
    {
        if (!IsDebugModeEnabled) return;
        DebugDrawUtility.DrawCollider(collider, color, duration);
    }
}
