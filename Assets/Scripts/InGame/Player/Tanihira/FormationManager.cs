using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

namespace Ingame.Tanihira
{
    public class FormationManager : NetworkBehaviour
    {
        [Header("隊列の設定")]
        [SerializeField] private Transform _firstFormationTransform;
        [SerializeField] private float _formationOffset = 1.0f;
        [Header("ワープ設定")] 
        [SerializeField] private float _warpSerchDistance = 10.0f;
        [SerializeField] private float _outFieldWarpHeight = 100.0f;
        [SerializeField] private float _inFieldWarpHeight = 10.0f;
        [SerializeField] private float _underWarpHeight = 30.0f;
        [SerializeField] private float _exceptionSerchDistance = 5.0f;
        [SerializeField] private LayerMask _raycastMask;
        private List<FriendBase> _friendsList = new List<FriendBase>();
        private List<FriendBase> _currentFriendsList = new List<FriendBase>();
        private Transform _playerTransform;
        private float _warpDuration = 0.5f;
        private CancellationTokenSource _cts;
        public List<FriendBase> CurrentFriendsList => _currentFriendsList;
        public List<FriendBase> FriendsList => _friendsList;
        

        private void Start()
        {
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// 友達登録処理
        /// </summary>
        /// <param name="friend"></param>
        public Transform Register(FriendBase friend)
        {
            //先頭の位置を返す
            if (_currentFriendsList.Count == 0)
            {
                _currentFriendsList.Add(friend);
                return _firstFormationTransform;
            }
            else //最後尾のオブジェクトのTransformを返す
            {
                Transform newDestination = _currentFriendsList.Last().FormationPos;
                _currentFriendsList.Add(friend);
                return newDestination;
            }
        }

        /// <summary>
        /// フレンドの隊列削除処理
        /// </summary>
        /// <param name="friend"></param>
        public void DeleteFriend(FriendBase friend)
        {
            int index = _currentFriendsList.IndexOf(friend);
            if (index >= 0)
            {
                _currentFriendsList.RemoveAt(index);
                SortFormation();
            }
        }

        /// <summary>
        /// ボスペンギンを返す
        /// </summary>
        public BossPenguinFriend GetBossFriend()
        {
            foreach (FriendBase friend in _currentFriendsList)
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
            if(_currentFriendsList.Count > 0)
            {
                for(int i = 0; i < _currentFriendsList.Count; i++)
                {
                    FriendBase friend = _currentFriendsList[i];

                    if (i == 0) //先頭の場合
                    {
                        friend.SetDestination(_firstFormationTransform);
                    }
                    else
                    {
                        friend.SetDestination(_currentFriendsList[i - 1].FormationPos);
                    }
                }
            }
        }

        /// <summary>
        /// 現在の隊列を登録する
        /// </summary>
        public void RegisterFriendFormation()
        {
            _friendsList.Clear();
            foreach (FriendBase friend in _currentFriendsList)
            {
                _friendsList.Add(friend);
            }
        }

        /// <summary>
        /// 隊列にいるフレンドをプレイヤーの近くにワープさせる
        /// </summary>
        public async void WarpFriendNearPlayer(Vector3 warpPosition, Quaternion warpRotation)
        {
            if(!HasStateAuthority)
                return;
            
            //フレンドのステートの切り替え
            foreach (FriendBase friend in _currentFriendsList)
            {
                //friend.Agent.isStopped = true;
                friend.Agent.enabled = false;
                friend.Animator.SetFloat("MoveBlend", 0);
                friend.IsWarp = true;
                friend.ChangeState(FriendState.Wait);
            }

            //少し待ってから移動させる
            await UniTask.Delay(TimeSpan.FromSeconds(_warpDuration), cancellationToken: _cts.Token);
            
            //フレンドを隊列のフレンドをワープさせる
            foreach (FriendBase friend in _currentFriendsList)
            {
                var fixedPos = warpPosition;
                NavMeshHit hit;
                RaycastHit raycastHit;
                if (NavMesh.SamplePosition(fixedPos, out hit, _warpSerchDistance, NavMesh.AllAreas))
                {
                    // NavMesh上にワープ
                    fixedPos = hit.position + Vector3.up * friend.Agent.baseOffset;
                }
                else
                {
                    if (Physics.Raycast(fixedPos, Vector3.down, out raycastHit, _underWarpHeight,_raycastMask))
                    {
                        if (NavMesh.SamplePosition(raycastHit.point, out hit, _warpSerchDistance, NavMesh.AllAreas))
                        {
                            fixedPos = hit.position + Vector3.up * friend.Agent.baseOffset;
                        }
                    }
                    else
                    {
                        fixedPos.y = _inFieldWarpHeight;
                        if (NavMesh.SamplePosition(fixedPos, out hit, _exceptionSerchDistance, NavMesh.AllAreas))
                        {
                            fixedPos = hit.position + Vector3.up * friend.Agent.baseOffset;
                        }
                    }
                }
                
                var networkTransform = friend.GetComponent<NetworkTransform>();
                networkTransform.Teleport(fixedPos, warpRotation);
                // NavMeshAgent と同期
                friend.Agent.Warp(fixedPos);
            }
        }

        [ContextMenu("FriendWarpOutSide")]
        public async void WarpFriendOutField()
        {
            if(!HasStateAuthority)
                return;
            
            //フレンドのステートの切り替え
            foreach (FriendBase friend in _friendsList)
            {
                //friend.Agent.isStopped = true;
                friend.Agent.enabled = false;
                friend.ChangeState(FriendState.None);
                friend.IsWarp = true;
            }
            
            //少し待ってから移動させる
            await UniTask.Delay(TimeSpan.FromSeconds(_warpDuration), cancellationToken: _cts.Token);
            
            Vector3 warpPos = new Vector3(0, _outFieldWarpHeight , 0);
            //フレンドを全員ワープさせる
            foreach (FriendBase friend in _friendsList)
            {
                var networkTransform = friend.GetComponent<NetworkTransform>();
                networkTransform.Teleport(warpPos);
            }
        }
        
#if UNITY_EDITOR       
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0, 1, 0.5f); // 半透明
            Vector3 startPos = new Vector3(0, _inFieldWarpHeight, 0);
            Gizmos.DrawWireCube(startPos, new Vector3(10, 1, 10));
        }
#endif
    }
}