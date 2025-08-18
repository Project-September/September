using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

namespace Ingame.Tanihira
{

    /// <summary>
    /// NPCの状態を定義するEnum
    /// </summary>
    public enum FriendState
    {
        Idle,
        Move,
        Attack,
        Chase,
        Stun
    }
    
    /// <summary>
    /// フレンド機能のベースクラス
    /// </summary>
    public class FriendBase : NetworkBehaviour
    {
        [SerializeField] protected Animator _animator;
        [SerializeField] protected Dictionary<FriendState, IFriendState> _friendStateMappings = new Dictionary<FriendState, IFriendState>();
        [SerializeField] protected FriendState _initialState = FriendState.Idle;
        [SerializeField, ReadOnly] protected Transform _destination;
        [SerializeField] protected FriendStatus _friendStatus;
        [SerializeField] protected Transform _formationPos;
        
        protected NavMeshAgent _agent;
        protected NetworkRunner _networkRunner;
        protected GameObject _ownerPlayer;
        protected FriendState _currentState;

        private static int _spawnCount;
        private FormationManager _formationManager;
        private NetworkMecanimAnimator _mecanimAnimator;
        
        // プロパティ
        public NavMeshAgent Agent => _agent;
        public Animator Animator => _animator;
        public FormationManager FormationManager => _formationManager;
        public Transform Destination => _destination;
        public NetworkRunner NetworkRunner => _networkRunner;
        public Transform FormationPos => _formationPos;
        public FriendStatus FriendStatus => _friendStatus;
        public FriendState CurrentState => _currentState;
        public NetworkMecanimAnimator MecanimAnimator => _mecanimAnimator;

        protected virtual void Awake()
        {
            _networkRunner = FindFirstObjectByType<NetworkRunner>();
            if (_networkRunner == null)
            {
                Debug.LogError("NetworkRunnerがありません");
            }
            if (!_networkRunner.IsServer) return;
        }
        
        protected virtual void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _mecanimAnimator = GetComponent<NetworkMecanimAnimator>();
            InitializeStates();
            ChangeState(_initialState);
        }

        public override void FixedUpdateNetwork()
        {
            if (!_networkRunner.IsServer && !HasInputAuthority) return;
            
            // 現在のステートのUpdateを呼び出し
            _friendStateMappings[_currentState]?.OnUpdate(this);
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected virtual void InitializeStates()
        {
            //アニメーションのルートモーションを不可
            if (_animator)
            {
                _animator.applyRootMotion = false;
            }
            
            _agent.enabled = false;
            _agent.enabled = true;
            _agent.updatePosition = true;
            _agent.updateRotation = true;
            InitializeAgent();
        }

        //ステータスをnavmeshに反映
        private void InitializeAgent()
        {
            _agent.angularSpeed = _friendStatus.FriendRotateSpeed;
            _agent.speed = _friendStatus.FriendMoveSpeed;
        }

        /// <summary>
        /// ステートを変更する
        /// </summary>
        /// <param name="newState">新しいステート</param>
        public virtual void ChangeState(FriendState newState)
        {
            if (_friendStateMappings[newState] == null)
            {
                Debug.LogWarning($"ステート {newState} が設定されていません");
                return;
            }
            
            // 現在のステートのOnExitを呼び出し、コンポーネントを無効化
            _friendStateMappings[_currentState]?.OnExit(this);
            
            // 新しいステートに変更
            _currentState = newState;
            
            // 新しいステートのOnEnterを呼び出し
            _friendStateMappings[_currentState]?.OnEnter(this);
        }
        
        /// <summary>
        /// 目的地を変更する
        /// </summary>
        /// <param name="destination"></param>
        public void SetDestination(Transform destination)
        {
            _destination = destination;
        }
        
        /// <summary>
        /// FormationManagerを設定
        /// </summary>
        /// <param name="formationManager">FormationManager</param>
        public void SetFormationManager(FormationManager formationManager)
        {
            _formationManager = formationManager;
        }

        /// <summary>
        /// オーナープレイヤーを設定
        /// </summary>
        /// <param name="ownerPlayer">オーナープレイヤー</param>
        public void SetOwnerPlayer(GameObject ownerPlayer)
        {
            _ownerPlayer = ownerPlayer;
        }
    }
}