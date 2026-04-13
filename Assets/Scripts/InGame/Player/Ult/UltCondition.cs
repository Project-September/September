using System;
using Fusion;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ult
{
    public class UltCondition : NetworkBehaviour, IUltCondition
    {
        [SerializeField] private int _requiredScore = 1000;

        /// <summary> 直近の必殺技発動時のスコア </summary>
        [Networked] private int PrevScore { get; set; }
        
        private int _currentScore;
        
        public int RemainingScore => Mathf.Clamp(_requiredScore - (_currentScore - PrevScore), 0, _requiredScore);
        public float Progress => Mathf.Clamp01((float)(_currentScore - PrevScore) / _requiredScore);
        
        public event Action OnProgressChanged;

        public bool IsAvailable()
        {
            return _currentScore - PrevScore >= _requiredScore;
        }

        public void OnUltActivated()
        {
            PrevScore = _currentScore;
            if (HasStateAuthority)
            {
                RPC_OnUltActivated();
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_OnUltActivated()
        {
            OnProgressChanged?.Invoke();
        }
        
        private void Start()
        {
            // スコアの変動を監視
            PlayerDatabase.Instance.ChangedDataAction += dict =>
            {
                if (!dict.TryGet(Object.InputAuthority, out var playerData))
                {
                    Debug.LogError("[UltCondition] PlayerData is not found");
                    return;
                }
                _currentScore = playerData.Score;
                OnProgressChanged?.Invoke();
            };
        }
    }
}