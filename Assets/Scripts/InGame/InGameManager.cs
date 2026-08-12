using System;
using System.Collections.Generic;
using System.Threading;
using Fusion;
using NaughtyAttributes;
using September.Common;
using September.InGame.Rules;
using September.InGame.UI;
using UnityEngine;

namespace September.InGame.Common
{
    public class InGameManager : NetworkStateMachineOwner<InGameManager>, IRegisterableService
    {
        [Header("Timer Settings"), SerializeField, Label("TimerData")]
        private GameTimerData _timerData;
        [Header("Game Settings"), SerializeField]
        private GameRule _gameRule;

        [SerializeField] private bool _isTutorialMode = false;
        private readonly Dictionary<PlayerRef, NetworkObject> _playerDataDic = new();

        private NetworkRunner _networkRunner;
        public IReadOnlyDictionary<PlayerRef, NetworkObject> PlayerDataDic => _playerDataDic;
        public GameTimerData TimerData => _timerData;
        public IGameRule GameRule => _gameRule;
        public CancellationTokenSource Cts { get; private set; }

        public System.Action GameStarted { get; set; }
        public System.Action<PlayerRef, PlayerRef> PlayerKilled { get; set; }

        /// <summary>
        /// 現在のゲーム状態名を取得する
        /// </summary>
        public string CurrentStateName => _stateMachine?.CurrentStateName ?? "";

        public Action GameEnded { get; set; }

        private void Start()
        {
            StaticServiceLocator.Instance.Register(this);
        }

        public void Register(ServiceLocator locator)
        {
            //locator.Register(this);
        }

        public override void Spawned()
        {
            Debug.Log($"[InGameManager] Spawned called. _states.Length={_states.Length}");
            Cts = new CancellationTokenSource();
            _networkRunner = FindFirstObjectByType<NetworkRunner>();
            if (_networkRunner == null) Debug.LogError("NetworkRunnerがありません");
            if (_states.Length > 0) base.Spawned();
        }

        protected override void InitializeStateMachine()
        {
            //  ステートマシン初期設定
            _stateMachine.AddTransition<TutorialState, PreparationState>((int)StateEventId.Finish);
            _stateMachine.AddTransition<PreparationState, PlayingState>((int)StateEventId.Finish);
            _stateMachine.AddTransition<PlayingState, EndingState>((int)StateEventId.Finish);
            if (_isTutorialMode)
            {
                _stateMachine.SetStartState<TutorialState>();
            }
            else
            {
                _stateMachine.SetStartState<PreparationState>();
            }
        }

        public void AddPlayerObject(PlayerRef playerRef, NetworkObject networkObject)
        {
            _playerDataDic.Add(playerRef, networkObject);
        }
    }
}
