using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    public static class TimeEx
    {
        public static async UniTask SetSlow(float timeScale, float duration)
        {
            var originalTimeScale = Time.timeScale;
            Time.timeScale = timeScale;
            await UniTask.WaitForSeconds(duration, true);
            Time.timeScale = originalTimeScale;
        }
    }
}