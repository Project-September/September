using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FormationManager : MonoBehaviour
    {
        [SerializeField] private Transform _firstFormationTransform;
        private List<FriendBase> _friendsList = new List<FriendBase>();

        private void Start()
        {
            if(!_firstFormationTransform)
                Debug.LogError("First formation transform is not set");
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
                Transform newDestination = _friendsList.Last().gameObject.transform;
                return newDestination;
            }
        }

        /// <summary>
        /// フレンドの削除処理
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
                        friend.SetDestination(_firstFormationTransform);
                    }
                    else
                    {
                        friend.SetDestination(_friendsList[i - 1].gameObject.transform);
                    }
                }
            }
        }
    }
}