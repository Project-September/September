using System.Collections.Generic;
using UnityEngine;

namespace September.InGame.Kraken
{
    public static class IKFollowerDebug
    {
        public static void DebugDrawLine(IReadOnlyList<Vector3> points, Color color)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 p0 = points[i];
                Vector3 p1 = points[i + 1];
                Debug.DrawLine(p0, p1, color);
            }
        }

        public static void DebugDrawLine(IReadOnlyList<IKFollower.Point> points, Color color)
        {
            DebugDrawLine(IKFollower.Point.ConvertToVector(points), color);
        }

        public static void DebugDrawSpheres(IReadOnlyList<Vector3> points, float radius, Color color)
        {
            foreach (var p in points)
            {
                DebugDrawUtility.DrawWireSphere(p, radius, color);
            }
        }

        public static void DebugDrawSpheres(IReadOnlyList<IKFollower.Point> points, float radius, Color color)
        {
            DebugDrawSpheres(IKFollower.Point.ConvertToVector(points), radius, color);
        }

        public static void DebugDrawNormals(IReadOnlyList<IKFollower.Point> points, Color color)
        {
            foreach (var p in points)
            {
                Debug.DrawRay(p.Position, p.Rotation * Vector3.up, color);
            }
        }
    }
}
