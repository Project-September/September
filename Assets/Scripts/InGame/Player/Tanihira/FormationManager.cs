using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ingame.Tanihira
{
    public class FormationManager : MonoBehaviour
    {
        [SerializeField] private Transform _firstFormationTransform;
        [SerializeField] private float _formationOffset;
        [SerializeField] private float _offsetZ;
        private List<FriendBase> _friendsList = new List<FriendBase>();
        private List<Transform> _friendsDestinationList = new List<Transform>();

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
            _friendsList.Add(friend);
            Transform newDestination;
            
            //先頭に登録された位置を返す
            if (_friendsDestinationList.Count == 0)
            {
                _friendsDestinationList.Add(_firstFormationTransform);
                return _firstFormationTransform;
            }
            else
            {
                //隊列の最後尾を取得
                var lastPos = _friendsDestinationList.Last();
                //オフセットを付けた位置
                Vector3 offset = new Vector3(0, 0, -_offsetZ);
                //新しいTransformを生成
                GameObject destinationObj = new GameObject("Friend Destination");
                newDestination = destinationObj.transform;
                //同じ親に設定
                destinationObj.transform.SetParent(_firstFormationTransform.parent);
                //ローカル座標と配置
                newDestination.position = lastPos.position + offset;
                newDestination.position = _firstFormationTransform.position;
                //リストに追加
                _friendsDestinationList.Add(newDestination);
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
                //対応するTransformも削除
                if (index < _friendsDestinationList.Count && _friendsDestinationList[index] != _firstFormationTransform)
                {
                    Destroy(_friendsDestinationList[index].gameObject);
                    _friendsDestinationList.RemoveAt(index);
                }
            }
        }
    }
}