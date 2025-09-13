using System;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Interact;
using InGame.Player;
using Ingame.Tanihira;
using UnityEngine;

namespace InGame.Exhibit
{
    [Serializable]
    public class OpticalCamouflageInteractEffect : CharacterInteractEffectBase
    {
        public float _duration;
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            PlayerRef playerRef = PlayerRef.FromEncoded(context.Interactor);
            
            // Runnerからplayerを取得する
            if (target.Runner.TryGetPlayerObject(playerRef, out var playerNetworkObject))
            {
                StartOpticalCamouflage(playerNetworkObject);
                StopOpticalCamouflage(playerNetworkObject).Forget();
                
                //FormationManagerを持っている場合には条件分岐
                if (playerNetworkObject.TryGetComponent<FormationManager>(out var formationManager))
                {
                    var friendList = formationManager.CurrentFriendsList;
                    foreach (var friend in friendList)
                    {
                        if (friend.TryGetComponent<NetworkObject>(out var friendNetworkObject))
                        {
                            StartOpticalCamouflage(friendNetworkObject);
                            StopOpticalCamouflage(friendNetworkObject).Forget();
                        }
                    }
                }
                else
                {
                    Debug.Log("FormationManager not found");
                }
            }
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new OpticalCamouflageInteractEffect
            {
                _duration = _duration,
            };
        }
        private void StartOpticalCamouflage(NetworkObject player)
        {
            if (player.TryGetComponent<PlayerRenderer>(out var playerRenderer))
            {
                playerRenderer.Rpc_StartOpticalCamouflage();
            }
        }

        private async UniTaskVoid StopOpticalCamouflage(NetworkObject player)
        {
            await UniTask.WaitForSeconds(_duration);
            if (player.TryGetComponent<PlayerRenderer>(out var playerRenderer))
            {
                playerRenderer.Rpc_StopOpticalCamouflage();
            }
        }
    }
}