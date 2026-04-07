using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;

namespace InGame.Player.Ult
{
    /// <summary>
    /// カットインの基底クラス
    /// </summary>
    public abstract class CutInAnimatorBase : NetworkBehaviour
    {
        public abstract UniTask PlayCutInAnimation(CancellationToken token); 
    }
}