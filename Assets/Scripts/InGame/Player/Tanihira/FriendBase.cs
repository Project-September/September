using System.Collections.Generic;
using System.Linq;
using CRISound;
using Fusion;
using InGame.Health;
using September.Common;
using September.InGame.Effect;
using UnityEngine;
using UnityEngine.AI;

namespace Ingame.Tanihira
{

    /// <summary>
    /// NPCの状態を定義するEnum
    /// </summary>
    public enum FriendState
    {
        None,
        Idle,
        Move,
        Attack,
        Chase,
        Stun,
        Wait
    }
    
    /// <summary>
    /// フレンド機能のベースクラス
    /// </summary>
    public class FriendBase : NetworkBehaviour, IFriendBuff
    {
        [SerializeField] protected Animator _animator;
        [SerializeField] protected Dictionary<FriendState, IFriendState> _friendStateMappings = new Dictionary<FriendState, IFriendState>();
        [SerializeField] protected FriendState _initialState = FriendState.Idle;
        [SerializeField, ReadOnly] protected Transform _destination;
        [SerializeField] protected FriendStatus _friendStatus;
        [SerializeField] protected Transform _formationPos;
        [SerializeField] protected HitChecker _hitChecker;
        [SerializeField, ReadOnly] protected FriendState _currentState;
        [SerializeField] private GameObject _tutankhamen;
        [SerializeField] private Transform _attackEffectPos;
        [Networked] private NetworkBool HasMask { get; set; }
        
        protected NavMeshAgent _agent;
        protected NetworkRunner _networkRunner;
        protected NetworkObject _ownerPlayer;
        protected NetworkMecanimAnimator _mecanimAnimator;
        protected FormationManager _formationManager;
        protected FriendState _waitStockState;
        protected FriendStatus _currentStatus;
        protected EffectSpawner _effectSpawner;

        private static int _spawnCount;
        private bool _isAttack;
        
        // プロパティ
        public NavMeshAgent Agent => _agent;
        public Animator Animator => _animator;
        public FormationManager FormationManager => _formationManager;
        public Transform Destination => _destination;
        public NetworkRunner FriendRunner => _networkRunner;
        public Transform FormationPos => _formationPos;
        public FriendStatus CurrentFriendStatus => _currentStatus;
        public FriendState CurrentState => _currentState;
        public NetworkMecanimAnimator MecanimAnimator => _mecanimAnimator;
        public bool IsAttack => _isAttack;
        public FriendState WaitStockState => _waitStockState;
        public NetworkObject OwnerPlayer => _ownerPlayer;
        public GameObject Tutankhamen => _tutankhamen;
        
        public override void Spawned()
        {
            _currentStatus = _friendStatus.Clone();
            //Noneステートの設定
            _friendStateMappings[FriendState.None] = new FriendNoneState();
            _agent = GetComponent<NavMeshAgent>();
            _mecanimAnimator = GetComponent<NetworkMecanimAnimator>();
            
            InitializeStates();
            ChangeState(_initialState);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            
            // 現在のステートのUpdateを呼び出し
            _friendStateMappings[_currentState]?.OnUpdate(this);
        }

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected virtual void InitializeStates()
        {
            //ヒット処理
            _hitChecker.OnHit -= OnHitEnemy;
            _hitChecker.OnHit += OnHitEnemy;
            
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
            _agent.angularSpeed = _currentStatus.FriendRotateSpeed;
            _agent.speed = _currentStatus.FriendFormationSpeed;
            _agent.acceleration = _currentStatus.FriendAccleration;
        }

        /// <summary>
        /// ステートを変更する
        /// </summary>
        /// <param name="newState">新しいステート</param>
        public virtual void ChangeState(FriendState newState)
        {
            //Noneの時には強制的にステートを変更させる
            if (newState == FriendState.None)
            {
                // 現在のステートのOnExitを呼び出し、コンポーネントを無効化
                _friendStateMappings[_currentState]?.OnExit(this);
                // 新しいステートに変更
                _currentState = newState;
                return;
            }
            
            if (_currentState == FriendState.Wait)
            {
                //attackの場合はchaseに変えておく
                if(newState == FriendState.Attack)
                {
                    newState = FriendState.Chase;
                }
                
                _waitStockState = newState;
            }
            
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
        public void SetOwnerPlayer(NetworkObject ownerPlayer)
        {
            _ownerPlayer = ownerPlayer;
        }
        
        private void OnHitEnemy(Collider hitInfo)
        {
            if (!HasStateAuthority) return;
            var targetObj = hitInfo.GetComponentInParent<NetworkObject>();
            if (targetObj == null || targetObj == _ownerPlayer) return;
            //隊列の味方も除外
            if (targetObj.TryGetComponent<FriendBase>(out var friend))
            {
                if (_formationManager != null && _formationManager.FriendsList != null && _formationManager.FriendsList.Contains(friend))
                    return;
            }
            
            var damageable = hitInfo.GetComponentInParent<IDamageable>();
            if (damageable == null) return;
            var hitData = new HitData(
                HitActionType.Damage,
                _currentStatus.AttackPower,
                _ownerPlayer.InputAuthority,
                damageable.OwnerPlayerRef);
            damageable.TakeHit(ref hitData);
            //エフェクトを出す
            PlayEffect();
        }
        
        private void PlayEffect()
        {
            // Effect生成処理
            _effectSpawner ??= StaticServiceLocator.Instance.Get<EffectSpawner>();
            _effectSpawner?.RequestPlayOneShotEffect(EffectType.PenguinAttack, _attackEffectPos.position,
                _attackEffectPos.rotation);
            // 音量再生
            //CRIAudio.PlaySE("ALLCue", SoundCues.SE.Penguin_Attack);
        }

        public void FinishWaitTime()
        {
            if (_waitStockState == FriendState.None)
            {
                ChangeState(FriendState.Move);
            }
            else
            {
                ChangeState(_waitStockState);
                _waitStockState = FriendState.None;
            }
        }
        
        //アニメーションイベント用
        public void StartAttack()
        {
            _isAttack = true;
        }

        public void EndAttack()
        {
            _isAttack = false;
        }

        public override void Render()
        {
            if(_tutankhamen)
                _tutankhamen.SetActive(HasMask);
        }
        public void SetMask(bool value)
        {
            if (!HasStateAuthority) return;
            HasMask = value;
        }

        public void StartBuff(float buffRate)
        {
            _currentStatus.FriendFormationSpeed *= buffRate;
            _currentStatus.FriendChaseSpeed *= buffRate;
            _currentStatus.AttackPower = (int)(_currentStatus.AttackPower * buffRate);
            ApplyStatus();
        }

        public void StopBuff()
        {
            _currentStatus.FriendFormationSpeed = _friendStatus.FriendFormationSpeed;
            _currentStatus.FriendChaseSpeed = _friendStatus.FriendChaseSpeed;
            _currentStatus.AttackPower = _friendStatus.AttackPower;
            ApplyStatus();
        }

        private void ApplyStatus()
        {
            //現在のステートによってスピードを反映する
            switch (_currentState)
            {
                case FriendState.Move:
                    _agent.speed = _currentStatus.FriendFormationSpeed;
                    break;
                case FriendState.Chase:
                    _agent.speed = _currentStatus.FriendChaseSpeed;
                    break;
                default:
                    break;
            }
        }
    }
}