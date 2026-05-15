using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class DebugDrawUtility
{
    [Conditional("UNITY_EDITOR")]
    public static void DrawWireSphere(Vector3 center, float radius, Color color, float duration = 0f)
    {
        int segments = 16;
        float angle = 360f / segments;

        // XY 平面
        for (int i = 0; i < segments; i++)
        {
            float theta1 = Mathf.Deg2Rad * angle * i;
            float theta2 = Mathf.Deg2Rad * angle * (i + 1);
            var p1 = center + new Vector3(Mathf.Cos(theta1), Mathf.Sin(theta1), 0) * radius;
            var p2 = center + new Vector3(Mathf.Cos(theta2), Mathf.Sin(theta2), 0) * radius;
            Debug.DrawLine(p1, p2, color, duration);
        }

        // YZ 平面
        for (int i = 0; i < segments; i++)
        {
            float theta1 = Mathf.Deg2Rad * angle * i;
            float theta2 = Mathf.Deg2Rad * angle * (i + 1);
            var p1 = center + new Vector3(0, Mathf.Cos(theta1), Mathf.Sin(theta1)) * radius;
            var p2 = center + new Vector3(0, Mathf.Cos(theta2), Mathf.Sin(theta2)) * radius;
            Debug.DrawLine(p1, p2, color, duration);
        }

        // XZ 平面
        for (int i = 0; i < segments; i++)
        {
            float theta1 = Mathf.Deg2Rad * angle * i;
            float theta2 = Mathf.Deg2Rad * angle * (i + 1);
            var p1 = center + new Vector3(Mathf.Cos(theta1), 0, Mathf.Sin(theta1)) * radius;
            var p2 = center + new Vector3(Mathf.Cos(theta2), 0, Mathf.Sin(theta2)) * radius;
            Debug.DrawLine(p1, p2, color, duration);
        }
    }
    
    [Conditional("UNITY_EDITOR")]
    public static void DrawWireCapsule(Vector3 p0, Vector3 p1, float radius, Color color, float duration = 0f)
    {
        // 中心位置を使って、球体を描画
        DrawWireSphere(p0, radius, color, duration);
        DrawWireSphere(p1, radius, color, duration);

        var capsuleDirection = (p0 - p1).normalized;
        var rot = Quaternion.FromToRotation(Vector3.up, capsuleDirection);

        // 4方向をつなぐ
        Vector3[] directions = {
            Vector3.right, Vector3.back, Vector3.forward, Vector3.left
        };

        foreach (var dir in directions)
        {
            var offset = rot * dir * radius;
            Debug.DrawLine(p0 + offset, p1 + offset, color, duration);
        }
    }

    [Conditional("UNITY_EDITOR")]
    public static void DrawOrientedWireBox(Vector3 center, Vector3 halfExtents, Quaternion rot, Color col, float duration)
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
        return;

        static void DrawEdge(Vector3 a, Vector3 b, Color col, float duration)
        {
            Debug.DrawLine(a, b, col, duration, false);
        }

        static Vector3[] GetBoxCorners(Vector3 center, Vector3 halfExtents, Quaternion rot)
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
}