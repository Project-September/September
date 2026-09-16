using System;
using Cysharp.Threading.Tasks;
using Fusion;

namespace September.InGame.Performances
{
    /// <summary>
    /// ゲーム開始時の演出
    /// </summary>
    public interface IGameStartPerformance
    {
        public bool Enabled { get; }

        public struct Context
        {
            public NetworkRunner Runner;
            public Action<bool, bool, bool> ToggleInputs;
        }

        public UniTask RunPerformance(Context ctx);
    }
}
