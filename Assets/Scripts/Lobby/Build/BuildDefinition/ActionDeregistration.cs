using System;

namespace September.Common
{
    /// <summary>登録解除を管理するクラス</summary>
    public sealed class ActionDeregistration : IDisposable
    {
        Action _deregistration;

        public ActionDeregistration(Action deregistration)
        {
            _deregistration = deregistration;
        }

        public void Dispose()
        {
            _deregistration?.Invoke();
        }
    }
}
