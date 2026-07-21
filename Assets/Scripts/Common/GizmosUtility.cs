using UnityEngine;

namespace September.Common
{
    public static class GizmosUtility
    {
        public static void DrawHorizontalCross(Vector3 center, float size)
        {
            float halfSize = size * 0.5f;
            Gizmos.DrawLine(center - Vector3.right * halfSize, center + Vector3.right * halfSize);
            Gizmos.DrawLine(center - Vector3.forward * halfSize, center + Vector3.forward * halfSize);
        }
    }
}