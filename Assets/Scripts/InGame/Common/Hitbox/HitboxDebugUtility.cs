#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

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
    /// そのフレームだけ、単一のワイヤーフレームBoxを描画する
    /// </summary>
    public static void DrawBoxOneFrame(
        Vector3 center,
        Vector3 halfExtents,
        Quaternion rot,
        Color color)
    {
        // duration=0 で1フレームだけ表示
        DrawOrientedWireBox(center, halfExtents, rot, color, 0f);
    }

    // ここから下はそのまま流用
    private static void DrawOrientedWireBox(Vector3 center, Vector3 halfExtents, Quaternion rot, Color col, float duration)
    {
        var c = GetBoxCorners(center, halfExtents, rot);
        // 底面
        DrawEdge(c[0], c[1], col, duration);
        DrawEdge(c[1], c[3], col, duration);
        DrawEdge(c[3], c[2], col, duration);
        DrawEdge(c[2], c[0], col, duration);
        // 上面
        DrawEdge(c[4], c[5], col, duration);
        DrawEdge(c[5], c[7], col, duration);
        DrawEdge(c[7], c[6], col, duration);
        DrawEdge(c[6], c[4], col, duration);
        // 縦
        DrawEdge(c[0], c[4], col, duration);
        DrawEdge(c[1], c[5], col, duration);
        DrawEdge(c[2], c[6], col, duration);
        DrawEdge(c[3], c[7], col, duration);
    }

    private static void DrawEdge(Vector3 a, Vector3 b, Color col, float duration)
    {
        Debug.DrawLine(a, b, col, duration, false);
    }

    private static Vector3[] GetBoxCorners(Vector3 center, Vector3 halfExtents, Quaternion rot)
    {
        Vector3 x = rot * new Vector3(halfExtents.x, 0, 0);
        Vector3 y = rot * new Vector3(0, halfExtents.y, 0);
        Vector3 z = rot * new Vector3(0, 0, halfExtents.z);

        var c = new Vector3[8];
        c[0] = center - x - y - z; c[1] = center + x - y - z;
        c[2] = center - x - y + z; c[3] = center + x - y + z;
        c[4] = center - x + y - z; c[5] = center + x + y - z;
        c[6] = center - x + y + z; c[7] = center + x + y + z;
        return c;
    }
}
