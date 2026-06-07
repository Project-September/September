using System;
using System.Collections.Generic;

namespace September.Common
{
    /// <summary>登録解除を管理するクラス</summary>
    public sealed class ActionDisposable : IDisposable
    {
        readonly List<Action> _disposes = new();

        public void AddActionDisposing(Action act)
        {
            if (_disposes.Contains(act)) return;
            _disposes.Add(act);
        }

        public void Dispose()
        {
            foreach (Action dispose in _disposes)
            {
                dispose();
            }
        }
    }
}
