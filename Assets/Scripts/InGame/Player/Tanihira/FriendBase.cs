using System.Collections;
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
        Patroll,
        Chase,
        Dead
    }
    
    /// <summary>
    /// ステートとMonoBehaviourの対応付けを行うクラス
    /// </summary>
    [System.Serializable]
    public class FriendStateMapping
    {
        [SerializeField] public FriendState stateType;
        [SerializeField] public FriendStateBase stateComponent;
    }
    
    /// <summary>
    /// フレンド機能のベースクラス
    /// </summary>
    public class FriendBase : NetworkBehaviour
    {
        private FormationManager _formationManager;

        [SerializeField] protected Animator _animator;
        [SerializeField] protected FriendStateMapping[] _friendStateMappings;
        [SerializeField] protected FriendState _initialState = FriendState.Idle;
        [SerializeField] protected Transform _destination;
        [SerializeField] protected FriendStatus _friendStatus;
        
        protected NavMeshAgent _agent;
        protected NetworkRunner _networkRunner;
        protected GameObject _ownerPlayer;
        
        //ステートマシン関連
        protected FriendState _currentState;
        protected FriendStateBase _currentStateInstance;
        protected FriendStateBase[] _stateInstances;
        
        // プロパティ
        public NavMeshAgent Agent => _agent;
        public Animator Animator => _animator;
        public FormationManager FormationManager => _formationManager;
        public Transform Destination => _destination;
        public NetworkRunner NetworkRunner => _networkRunner;

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
            InitializeStates();
            ChangeState(_initialState);
        }

        public override void FixedUpdateNetwork()
        {
            if (!_networkRunner.IsServer && !HasInputAuthority) return;
            
            // 現在のステートのUpdateを呼び出し
            _currentStateInstance?.OnUpdate();
        }

        /// <summary>
        /// ステートを初期化する
        /// </summary>
        protected virtual void InitializeStates()
        {
            //アニメーションのルートモーションを不可
            if (_animator)
            {
                _animator.applyRootMotion = false;
            }
            _stateInstances = new FriendStateBase[System.Enum.GetValues(typeof(FriendState)).Length];
            
            //ステート関連の初期化
            foreach (var mapping in _friendStateMappings)
            {
                if (mapping.stateComponent == null)
                {
                    Debug.LogWarning($"ステート {mapping.stateType} のコンポーネントが設定されていません");
                    continue;
                }
                
                mapping.stateComponent.Initialize(this, _friendStatus);
                int index = (int)mapping.stateType;
                
                if (_stateInstances[index] != null)
                {
                    Debug.LogWarning($"ステート {mapping.stateType} が重複して設定されています");
                }
                
                _stateInstances[index] = mapping.stateComponent;
            }
            
            _agent.enabled = false;
            _agent.enabled = true;
        }

        /// <summary>
        /// ステートを変更する
        /// </summary>
        /// <param name="newState">新しいステート</param>
        public virtual void ChangeState(FriendState newState)
        {
            var newStateComponent = _stateInstances[(int)newState];
            if (newStateComponent == null)
            {
                Debug.LogWarning($"ステート {newState} が設定されていません");
                return;
            }
            
            // 現在のステートのOnExitを呼び出し、コンポーネントを無効化
            if (_currentStateInstance != null)
            {
                _currentStateInstance.OnExit();
            }
            
            // 新しいステートに変更
            _currentState = newState;
            _currentStateInstance = newStateComponent;
            
            // 新しいステートのOnEnterを呼び出し
            _currentStateInstance.OnEnter();
        }

        /// <summary>
        /// 現在のステートを取得
        /// </summary>
        public FriendState GetCurrentState()
        {
            return _currentState;
        }


        [ContextMenu("ChangeDestination")]
        public void ChangeDestination()
        {
            SetDestination(_destination);
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