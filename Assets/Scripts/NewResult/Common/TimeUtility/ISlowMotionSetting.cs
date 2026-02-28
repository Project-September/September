using System.Threading;
using Cysharp.Threading.Tasks;

namespace September.NewResult
{
    public interface ISlowMotionSetting
    {
        UniTask PlaySlowMotion(CancellationToken token);
    }
}