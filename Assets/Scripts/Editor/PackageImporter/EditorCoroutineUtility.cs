using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace September.Editor.PackageImporter
{
    /// <summary>
    /// UnityEditor上でIEnumeratorベースの処理を実行するための簡易ユーティリティ
    /// EditorWindowはMonoBehaviourではない＝標準のStartCoroutineは使わない
    /// EditorApplication.updateを使って自前でMoveNextを回す
    /// ネストしたyield return（子コルーチン）には対応していない
    /// 呼び出し側→while(!op.isDone) yield return null; みたいに単純な待機のみで使う
    /// </summary>
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
                EditorApplication.update += Update;
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