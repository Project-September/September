using Fusion;

namespace InGame.Player.Ult
{
    /// <summary>
    /// カットインの基底クラス
    /// </summary>
    public abstract class CutInAnimatorBase : NetworkBehaviour
    {
        public bool IsCutInAnimationPlaying { get; protected set; }
        public abstract void RequestPlayCutInAnimation(); 
    }
}