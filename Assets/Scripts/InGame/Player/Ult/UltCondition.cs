using System;
using CRISound;
using Fusion;
using September.Common;
using UnityEngine;

namespace InGame.Player.Ult
{
    public class UltCondition : NetworkBehaviour, IUltCondition
    {
        [SerializeField] private int _requiredScore = 1000;

        /// <summary> 直近の必殺技発動時のスコア </summary>
        [Networked, OnChangedRender(nameof(OnPrevScoreChangedRender))] private int PrevScore { get; set; }
        
        private int _currentScore;
        private bool _wasAvailable;
        private PlayerDatabase _playerDatabase;
        
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
            _wasAvailable = false;
        }

        /// <summary>
        /// 必殺技発動時にUIを更新する用
        /// </summary>
        private void OnPrevScoreChangedRender()
        {
            _wasAvailable = IsAvailable();
            OnProgressChanged?.Invoke();
        }
        
        private void Start()
        {
            _wasAvailable = IsAvailable();

            // スコアの変動を監視
            _playerDatabase = PlayerDatabase.Instance;
            if (_playerDatabase != null)
            {
                _playerDatabase.ChangedDataAction += OnPlayerDataChanged;
            }
        }

        private void OnDestroy()
        {
            if (_playerDatabase != null)
            {
                _playerDatabase.ChangedDataAction -= OnPlayerDataChanged;
            }
        }

        private void OnPlayerDataChanged(NetworkDictionary<PlayerRef, SessionPlayerData> playerDataDictionary)
        {
            if (!playerDataDictionary.TryGet(Object.InputAuthority, out var playerData))
            {
                Debug.LogError("[UltCondition] PlayerData is not found");
                return;
            }

            _currentScore = playerData.Score;

            bool isAvailable = IsAvailable();
            if (Object.HasInputAuthority && !_wasAvailable && isAvailable)
            {
                CRIAudio.PlaySE(SoundCues.SE.Ult_Charge.Sheet, SoundCues.SE.Ult_Charge.Name);
            }

            _wasAvailable = isAvailable;
            OnProgressChanged?.Invoke();
        }
    }
}
