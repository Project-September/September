using Fusion;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ult
{
    public class UltCondition : MonoBehaviour, IUltCondition
    {
        [SerializeField] private int _requiredScore = 1000;

        /// <summary> 直近の必殺技発動時のスコア </summary>
        private int _prevScore;
        
        private int _currentScore;
        
        public int RemainingScore => Mathf.Clamp(_requiredScore - (_currentScore - _prevScore), 0, _requiredScore);
        public float Progress => Mathf.Clamp01((float)(_currentScore - _prevScore) / _requiredScore);

        public bool IsAvailable()
        {
            return _currentScore - _prevScore >= _requiredScore;
        }

        public void OnUltActivated()
        {
            _prevScore = _currentScore;
        }
        
        private void Start()
        {
            // スコアの変動を監視
            PlayerDatabase.Instance.ChangedDataAction += dict =>
            {
                var runner = NetworkRunner.Instances[0];
                if (runner == null)
                {
                    Debug.LogError("[UltCondition] NetworkRunner is not initialized");
                    return;
                }
                
                var localPlayer = runner.LocalPlayer;
                if (!dict.TryGet(localPlayer, out var playerData))
                {
                    Debug.LogError("[UltCondition] PlayerData is not found");
                    return;
                }
                _currentScore = playerData.Score;
            };
        }
    }
}