using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;

namespace InGame.Player.Ult
{
    /// <summary>
    /// カットインの抽象クラス
    /// </summary>
    public abstract class CutInAnimatorBase : NetworkBehaviour
    {
        public abstract UniTask PlayCutInAnimation(CancellationToken token); 
    }
}