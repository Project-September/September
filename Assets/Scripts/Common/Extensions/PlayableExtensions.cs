using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;

namespace Common.Extensions
{
    public static class PlayableExtensions
    {
        public static async UniTask WaitUntilEnd(this PlayableDirector playableDirector, CancellationToken token = default) 
        {
            await UniTask.WaitUntil(playableDirector, p => p.state != PlayState.Playing, cancellationToken: token);
        }

        public static async UniTask PlayAsync(this PlayableDirector playableDirector, CancellationToken token = default)
        {
            playableDirector.Play();
        
            await playableDirector.WaitUntilEnd(token);
        
            if (token.IsCancellationRequested)
            {
                playableDirector.Stop();
            }
        }
    }
}