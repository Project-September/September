using System;

namespace September.Common
{
    /// <summary>登録解除を管理するクラス</summary>
    public sealed class ActionDisposable : IDisposable
    {
        Action _dispose;

        public ActionDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose?.Invoke();
        }
    }
}
