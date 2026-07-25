using System.Text;
using UnityEngine;

namespace Common.Extensions
{
    public static class GameObjectExtensions
    {
        public static string GetHierarchyPath(this GameObject gameObject)
        {
            if (gameObject == null)
                return string.Empty;

            var current = gameObject.transform;
            var builder = new StringBuilder(current.name);

            while (current.parent != null)
            {
                current = current.parent;
                builder.Insert(0, current.name + "/");
            }

            return builder.ToString();
        }
    }
}
