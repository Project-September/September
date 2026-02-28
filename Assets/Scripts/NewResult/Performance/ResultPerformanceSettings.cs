using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace September.NewResult
{
    [CreateAssetMenu(fileName = "ResultPerformanceSettings", menuName = "ScriptableObjects/ResultPerformanceSettings")]
    public class ResultPerformanceSettings : ScriptableObject, ISlowMotionSetting
    {
        [Header("1位表示タイミング設定")]
        [SerializeField] private float _uiAnimationStartTime = 4.4f;

        [Header("BGM再生タイミング設定")]
        [SerializeField] private float _bgmStartTime;
        
        [Header("決めポーズ中の停止/スローモーション設定")]
        [SerializeField] private float _timeScale = 0.1f;
        [SerializeField] private float _duration = 1f;
        
        public float UIAnimationStartTime => _uiAnimationStartTime;
        public float BgmStartTime => _bgmStartTime;
        public float TimeScale => _timeScale;
        public float Duration => _duration;
        
        public async UniTask PlaySlowMotion(CancellationToken token)
        {
            await TimeEx.SetSlow(_timeScale, _duration, token);
        }
    }
}