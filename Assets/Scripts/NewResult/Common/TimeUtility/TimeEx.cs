using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public static class TimeEx
    {
        public static async UniTask SetSlow(float timeScale, float duration, CancellationToken token)
        {
            var originalTimeScale = Time.timeScale;
            
            try
            {
                Time.timeScale = timeScale;
                await UniTask.WaitForSeconds(duration, true, cancellationToken: token);
            }
            finally
            {
                Time.timeScale = originalTimeScale;
            }
        }
    }
}