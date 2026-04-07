using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.Common;
using UnityEngine;

namespace InGame.Player.Ult
{
    /// <summary>
    /// 必殺技用カットインの実装クラス
    /// </summary>
    [RequireComponent(typeof(AnimationClipPlayer))]
    public class UltCutInAnimator : CutInAnimatorBase
    {
        [SerializeField] private AnimationClip _cutInAnimation;
        [SerializeField] private AnimationClipPlayer _player;

        public override async UniTask PlayCutInAnimation(CancellationToken token)
        {
            if (!_cutInAnimation)
            {
                Debug.LogWarning("[CutInAnimator] カットインアニメーションが設定されていません", this);
                return;
            }

            if (!_player)
            {
                Debug.LogWarning("[CutInAnimator] AnimationClipPlayerがアサインされていません", this);
                return;
            }
            
            await _player.PlayClipAndWait(_cutInAnimation, token);
        }
    }
}