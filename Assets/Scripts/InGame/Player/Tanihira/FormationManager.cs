using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FormationManager : MonoBehaviour
    {
        [SerializeField] private Transform _firstFormationTransform;
        [SerializeField] private float _formationOffset = 1f;
        private List<FriendBase> _friendsList = new List<FriendBase>();
        private Transform _playerTransform;

        private void Start()
        {
            _playerTransform = GetComponent<Transform>();
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
                return _playerTransform;
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
        /// 先頭の友達を返すメソッド
        /// </summary>
        public FriendBase GetFirstFriend()
        {
            if( _friendsList.Count > 0)
            {
                return _friendsList.First();
            }
            
            return null;
        }

        //隊列の整理をする

        private void SortFormation()
        {
            if(_friendsList.Count > 0)
            {
                for(int i = 0; i < _friendsList.Count; i++)
                {
                    FriendBase friend = _friendsList[i];

                    if (i == 0) //先頭の場合
                    {
                        friend.SetDestination(_playerTransform);
                    }
                    else
                    {
                        friend.SetDestination(_friendsList[i - 1].FormationPos);
                    }
                }
            }
        }
    }
}