using Fusion;
using Ingame.Tanihira;
using UnityEngine;

namespace InGame.Tanihira
{
    public class FriendsSpawner : NetworkBehaviour
    {
        [Header("初期友達の設定")]
        [SerializeField] private Transform _firstSpawnPosition;
        [SerializeField] private FriendType[] _friendsTypes;
        [SerializeField] private FriendDatabase _friendDatabase;
        [SerializeField] private FormationManager _formationManager;
        [SerializeField] private GameObject _ownerPlayer;

        public override void Spawned()
        {
            Initialize();
        }

        //初期化処理
        private void Initialize()
        {
            //初期で登録されたフレンドを生成
            for (int i = 0; i < _friendsTypes.Length; i++)
            {
                Transform pos = _ownerPlayer.transform;
                pos.transform.position += new Vector3(i, 0, 0);
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
                FriendBase friend = Instantiate(prefab, spawnPosition.position, Quaternion.identity, null).GetComponent<FriendBase>();
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