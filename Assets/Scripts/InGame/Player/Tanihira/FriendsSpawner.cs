using Fusion;
using Ingame.Tanihira;
using UnityEngine;
using UnityEngine.AI;

namespace InGame.Tanihira
{
    public class FriendsSpawner : NetworkBehaviour
    {
        [Header("初期友達の設定")]
        [SerializeField] private FriendType[] _friendsTypes;
        [SerializeField] private FriendDatabase _friendDatabase;
        [SerializeField] private FormationManager _formationManager;
        [SerializeField] private GameObject _ownerPlayer;
        [SerializeField] private NetworkRunner _networkRunner;
        [SerializeField] private Transform _firstSpawnPoint;
        [SerializeField] private float _navmeshSerchRadius = 5.0f;

        public void Start()
        {
            Initialize();
        }

        //初期化処理
        private void Initialize()
        {
            _networkRunner = FindFirstObjectByType<NetworkRunner>();
            if (_networkRunner == null)
            {
                Debug.LogError("NetworkRunnerがありません");
            }
            if (!_networkRunner.IsServer) return;
            
            //初期で登録されたフレンドを生成
            for (int i = 0; i < _friendsTypes.Length; i++)
            {
                Transform pos = _firstSpawnPoint;
                //仮で３の間隔開けている（複数スポーンするときのオフセット）
                pos.transform.position += new Vector3(i * 3, 0, 0);
                SpawnFriend(_friendsTypes[i], pos);
            }
        }

        /// <summary>
        /// フレンドを生成
        /// </summary>
        public void SpawnFriend(FriendType friendType,Transform spawnPosition)
        {
            var prefab = _friendDatabase.GetFriendObject(friendType);
            
            if (prefab)
            {
                Vector3 fixedPos = spawnPosition.position; 
                NavMeshAgent navMeshAgent = prefab.GetComponent<NavMeshAgent>();
                NavMeshHit hit;
                if (NavMesh.SamplePosition(spawnPosition.position, out hit, _navmeshSerchRadius, NavMesh.AllAreas))
                {
                    // NavMesh上にワープ
                    fixedPos = hit.position + Vector3.up * navMeshAgent.baseOffset;
                }
                else
                {
                    Debug.LogWarning($"{friendType} はNavMesh上にスポーンできませんでした");
                }
                
                FriendBase friend = _networkRunner.Spawn(prefab, fixedPos, Quaternion.identity, null).GetComponent<FriendBase>();
                
                if (friend)
                {
                    // フレンドにFormationManagerとownerPlayerを設定
                    friend.SetFormationManager(_formationManager);
                    friend.SetOwnerPlayer(_ownerPlayer);
                    if (_formationManager)
                    {
                        //友達登録
                        var pos = _formationManager.Register(friend);
                        friend.SetDestination(pos);
                    }
                }
            }
            
        }
    }

}