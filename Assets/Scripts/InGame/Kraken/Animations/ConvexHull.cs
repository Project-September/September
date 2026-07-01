using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace September.InGame.Kraken
{
    public static class ConvexHull
    {
        // AI生成
        // Monotone Chainらしい
        struct HullPoint
        {
            public float u;
            public float y;
            public Vector3 position;
            public int index;
        }

        public static int[] BuildUpperHull(IReadOnlyList<Vector3> points)
        {
            if (points.Count <= 2) return Enumerable.Range(0, points.Count).ToArray();

            Vector3 horizontal =
                Vector3.ProjectOnPlane(
                    points[points.Count - 1] - points[0],
                    Vector3.up);

            if (horizontal.sqrMagnitude < 0.000001f)
            {
                for (int i = 1; i < points.Count; i++)
                {
                    horizontal =
                        Vector3.ProjectOnPlane(
                            points[i] - points[0],
                            Vector3.up);

                    if (horizontal.sqrMagnitude > 0.000001f)
                        break;
                }
            }

            horizontal.Normalize();

            List<HullPoint> projected = new();

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p = points[i];
                projected.Add(new HullPoint
                {
                    u = Vector3.Dot(p, horizontal),
                    y = p.y,
                    position = p,
                    index = i
                });
            }

            projected.Sort((a, b) => a.u.CompareTo(b.u));

            List<HullPoint> upper = new();

            foreach (var p in projected)
            {
                while (upper.Count >= 2)
                {
                    var a = upper[upper.Count - 2];
                    var b = upper[upper.Count - 1];

                    float cross =
                        (b.u - a.u) * (p.y - a.y) -
                        (b.y - a.y) * (p.u - a.u);

                    if (cross >= 0f)
                        upper.RemoveAt(upper.Count - 1);
                    else
                        break;
                }

                upper.Add(p);
            }

            return upper.Select(h => h.index).ToArray();
        }
    }
}