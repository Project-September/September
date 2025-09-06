using System.Collections.Generic;
using System.Linq;
using Fusion;
using September.Common;
using September.InGame.Effect;
using UnityEngine;
using UnityEngine.AI;

namespace Ingame.Tanihira
{
    public class FormationManager : NetworkBehaviour
    {
        [SerializeField] private Transform _firstFormationTransform;
        [SerializeField] private float _formationOffset = 1f;
        private List<FriendBase> _friendsList = new List<FriendBase>();
        private Transform _playerTransform;
        //private EffectSpawner _effectSpawner;
        public List<FriendBase> FriendsList => _friendsList;

        private void Start()
        {
            //_effectSpawner ??= StaticServiceLocator.Instance.Get<EffectSpawner>();
        }

        /// <summary>
        /// 友達登録処理
        /// </summary>
        /// <param name="friend"></param>
        public Transform Register(FriendBase friend)
        {
            //先頭の位置を返す
            if (_friendsList.Count == 0)
            {
                _friendsList.Add(friend);
                return _firstFormationTransform;
            }
            else //最後尾のオブジェクトのTransformを返す
            {
                Transform newDestination = _friendsList.Last().FormationPos;
                _friendsList.Add(friend);
                return newDestination;
            }
        }

        /// <summary>
        /// フレンドの隊列削除処理
        /// </summary>
        /// <param name="friend"></param>
        public void DeleteFriend(FriendBase friend)
        {
            int index = _friendsList.IndexOf(friend);
            if (index >= 0)
            {
                _friendsList.RemoveAt(index);
                SortFormation();
            }
        }

        /// <summary>
        /// ボスペンギンを返す
        /// </summary>
        public BossPenguinFriend GetBossFriend()
        {
            foreach (FriendBase friend in _friendsList)
            {
                if(friend.TryGetComponent<BossPenguinFriend>(out BossPenguinFriend bossPenguinFriend))
                    return bossPenguinFriend;
            }
            
            return null;
        }

        /// <summary>
        /// 隊列整理
        /// </summary>
        public void SortFormation()
        {
            if(_friendsList.Count > 0)
            {
                for(int i = 0; i < _friendsList.Count; i++)
                {
                    FriendBase friend = _friendsList[i];

                    if (i == 0) //先頭の場合
                    {
                        friend.SetDestination(_firstFormationTransform);
                    }
                    else
                    {
                        friend.SetDestination(_friendsList[i - 1].FormationPos);
                    }
                }
            }
        }

        /// <summary>
        /// 隊列にいるフレンドをワープさせる
        /// </summary>
        public void WarpFriend(Vector3 warpPosition, Quaternion warpRotation)
        {
            if(!HasStateAuthority)
                return;
            
            //フレンドを全員ワープさせる
            foreach (FriendBase friend in _friendsList)
            {
                friend.Agent.enabled = false;
                var fixedPos = warpPosition;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(warpPosition, out hit, 10.0f, NavMesh.AllAreas))
                {
                    // NavMesh上にワープ
                    fixedPos = hit.position + Vector3.up * friend.Agent.baseOffset;
                }
                else
                {
                    return;
                }
                
                var networkTransform = friend.GetComponent<NetworkTransform>();
                networkTransform.Teleport(fixedPos, warpRotation);
                friend.ChangeState(FriendState.Wait);
            }
        }
    }
}