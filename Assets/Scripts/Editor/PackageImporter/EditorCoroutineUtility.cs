using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor;

namespace September.Editor.PackageImporter
{
    internal static class EditorCoroutineUtility
    {
        private static readonly List<IEnumerator> _routines = new List<IEnumerator>();
        private static bool _subscribed;

        public static void Start(IEnumerator routine)
        {
            if (routine == null) return;

            _routines.Add(routine);

            if (!_subscribed)
            {
                EditorApplication.update += UpdateRowSource;
                _subscribed = true;
            }
        }

        private static void Update()
        {
            if (_routines.Count == 0) return;

            for (int i = _routines.Count - 1; i >= 0; i--)
            {
                var routine = _routines[i];
                bool moved;
                try
                {
                    moved = routine.MoveNext();
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                    moved = false;
                }

                if (!moved)
                {
                    _routines.RemoveAt(i);
                }
            }

            if (_routines.Count == 0)
            {
                EditorApplication.update -= Update;
                _subscribed = false;
            }
        }
    }
}