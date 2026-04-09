using System.Threading;
using Common.Extensions;
using Cysharp.Threading.Tasks;
using InGame.Common;
using UnityEngine;
using UnityEngine.Playables;

namespace InGame.Player.Ult
{
    /// <summary>
    /// 必殺技用カットインの実装クラス
    /// </summary>
    [RequireComponent(typeof(AnimationClipPlayer))]
    public class UltCutInAnimator : CutInAnimatorBase
    {
        [SerializeField] private PlayableDirector _playableDirector;

        public override async UniTask PlayCutInAnimation(CancellationToken token)
        {
            if (!_playableDirector)
            {
                Debug.LogWarning("[UltCutInAnimator] PlayableDirectorが設定されていません。必殺技カットインはスキップされます");
                return;
            }
            
            _playableDirector.Play();
            await _playableDirector.PlayAsync(token);
        }
    }
}