using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Fusion;
using Ingame.Tanihira;
using September.InGame.UI;

namespace InGame.Exhibit
{
    // RPC送信用
    public class TutankhamenInteractRPCInvoker : NetworkBehaviour
    {
        [Header("Tutankhamen Settings")]
        [SerializeField] private GameObject _tutankhamenHead;
        
        private float _maskDuration = 5f;
        private GameObject _instantiateMask;
        private bool _isMaskAttached;

        private List<FriendBase> _friendList = new List<FriendBase>();
        
        public bool IsMaskAttached => _isMaskAttached;
        
        [Rpc(RpcSources.StateAuthority,RpcTargets.All)]
        public void RPC_AttachHeadMask(NetworkObject player, float duration)
        {
            AttachHeadMask(player);
            if (player.TryGetComponent<FormationManager>(out FormationManager formationManager))
            {
                foreach (var friend in formationManager.FriendsList)
                {
                    _friendList.Add(friend);
                }
                
                if(_friendList.Count > 0)
                {
                    foreach (FriendBase friend in _friendList)
                    {
                        friend.SetMask(true);
                    }
                }
            }
            _maskDuration = duration;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_DestroyMask()
        {
            DestroyMask().Forget();
        }
        
        // 仮面を被る
        private void AttachHeadMask(NetworkObject player)
        {
            if (_tutankhamenHead == null)
            {
                Debug.LogError($"AttachHeadMask: No head mask attached to player {player}");
                return;
            }

            Transform head = null;
            Transform[] allChildren = player.GetComponentsInChildren<Transform>(true);
            // Playerの頭の位置を探す
            foreach (Transform child in allChildren)
            {
                if (child.CompareTag("Head"))
                {
                    head = child;
                    break;
                }
            }
            
            if (head == null)
            {
                Debug.LogError("AttachHeadMask head is null");
                return;
            }
            
            // 仮面をインスタンスして頭の子にする
            _instantiateMask = Instantiate(_tutankhamenHead, head.transform);
            _instantiateMask.transform.localPosition = Vector3.zero;
            _instantiateMask.transform.localRotation = Quaternion.identity;
            
            _instantiateMask.GetComponent<ParticleSystem>().Play();
            _isMaskAttached = true;
        }
        
        // 仮面をとる
        private async UniTaskVoid DestroyMask()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_maskDuration));

            if(_friendList.Count > 0)
            {
                foreach (FriendBase friend in _friendList)
                {
                    friend.StopBuff();
                    friend.SetMask(false);
                }
                _friendList.Clear();
            }

            if (_instantiateMask == null)
                return;
            
            Destroy(_instantiateMask);
            _instantiateMask = null;
            _isMaskAttached = false;
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ShowStatusUpUI(PlayerRef playerRef)
        {
            if (Runner.LocalPlayer == playerRef)
            {
                UIPresenter.I.ShowStatusUpUI(_maskDuration, StatusUpType.Tutankhamen);
            }
        }
    }
}