using Fusion;

namespace InGame.Player.Ult
{
    /// <summary>
    /// カットインの基底クラス
    /// </summary>
    public abstract class CutInAnimatorBase : NetworkBehaviour
    {
        /// <summary>
        /// カットインの秒数
        /// </summary>
        public abstract double Duration { get; }
        
        /// <summary>
        /// カットインを開始する
        /// </summary>
        public abstract void RequestPlayCutInAnimation();
    }
}