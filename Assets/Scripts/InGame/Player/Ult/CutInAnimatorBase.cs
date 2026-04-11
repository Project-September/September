using Fusion;

namespace InGame.Player.Ult
{
    /// <summary>
    /// カットインの基底クラス
    /// </summary>
    public abstract class CutInAnimatorBase : NetworkBehaviour
    {
        /// <summary>
        /// カットイン再生中かどうか。サーバー側からカットインが終了したか検知するために使用する
        /// </summary>
        public bool IsCutInAnimationPlaying { get; protected set; }
        public abstract void RequestPlayCutInAnimation(); 
    }
}