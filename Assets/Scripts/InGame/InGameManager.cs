using System;
using System.Collections.Generic;
using System.Threading;
using Fusion;
using NaughtyAttributes;
using September.Common;
using September.InGame.UI;
using UnityEngine;

namespace September.InGame.Common
{
    public class InGameManager : NetworkStateMachineOwner<InGameManager>, IRegisterableService
    {
        [SerializeField] private string _inGameBGMCueName = "Default_BGM";
        [Header("Timer Settings"), SerializeField, Label("TimerData")]
        
        private GameTimerData _timerData;
        
        [Header("他Playerを気絶させたときに得られるスコア"), SerializeField] 
        private int _stunScore;

        private readonly Dictionary<PlayerRef, NetworkObject> _playerDataDic = new();

        private NetworkRunner _networkRunner;
        public IReadOnlyDictionary<PlayerRef, NetworkObject> PlayerDataDic => _playerDataDic;
        public GameTimerData TimerData => _timerData;
        public CancellationTokenSource Cts { get; private set; }

        public int StunScore => _stunScore;
        public string InGameBGMCueName => _inGameBGMCueName;
        public string CurrentBGM { get; set; }

        public System.Action GameStarted { get; set; }

        /// <summary>
        /// 現在のゲーム状態名を取得する
        /// </summary>
        public string CurrentStateName => _stateMachine?.CurrentStateName ?? "";

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
            Cts = new CancellationTokenSource();
            _networkRunner = FindFirstObjectByType<NetworkRunner>();
            if (_networkRunner == null) Debug.LogError("NetworkRunnerがありません");
            base.Spawned();
        }

        protected override void InitializeStateMachine()
        {
            //  ステートマシン初期設定
            _stateMachine.AddTransition<PreparationState, PlayingState>((int)StateEventId.Finish);
            _stateMachine.AddTransition<PlayingState, EndingState>((int)StateEventId.Finish);
            _stateMachine.SetStartState<PreparationState>();
        }

        public void AddPlayerObject(PlayerRef playerRef, NetworkObject networkObject)
        {
            _playerDataDic.Add(playerRef, networkObject);
        }
    }
}