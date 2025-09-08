using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Fusion;
using Ingame.Tanihira;

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

        private BossPenguinFriend _bossPenguin;
        
        public bool IsMaskAttached => _isMaskAttached;

        [Rpc(RpcSources.StateAuthority,RpcTargets.All)]
        public void RPC_AttachHeadMask(NetworkObject player, float duration)
        {
            if (player.TryGetComponent<FormationManager>(out FormationManager formation))
            {
                _bossPenguin = formation.GetBossFriend();
                if(_bossPenguin)
                {
                    _bossPenguin.StartBuff();
                    _isMaskAttached = true;
                }
            }
            else
            {
                AttachHeadMask(player);
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
            
            // Playerの頭の位置を探す
            // ToDo : FIndは負荷が高いので別の手段で生成する(ここでAddComponentでいいかも)
            Transform head = player.transform.Find("Head");
            
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

            if(_bossPenguin)
            {
                DestoryPenguinMask(_bossPenguin);
                return;
            }   

            if (_instantiateMask == null)
                return;
            
            Destroy(_instantiateMask);
            _instantiateMask = null;
            
            _isMaskAttached = false;
        }

        private void DestoryPenguinMask(BossPenguinFriend bossPenguin)
        {
            bossPenguin.StopBuff();
        }
    }
}