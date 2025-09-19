using System;
using Fusion;
using InGame.Interact;
using InGame.Player;
using UnityEngine;

namespace Ingame.Exhibit
{
    [Serializable]
    public class SateliteCannonInteractEffect : CharacterInteractEffectBase
    {
        public SateliteCannonInteractRPCInvoker SateliteCannonInteractRPCInvoker;
        public Transform _lookPoint;
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            PlayerRef playerRef = PlayerRef.FromEncoded(context.Interactor);
            SateliteCannonInteractRPCInvoker.Rpc_RequestInteraction(playerRef);
            //プレイヤーの向きを後ろにする
            if (target.Runner.TryGetPlayerObject(playerRef, out NetworkObject playerNetworkObject))
            {
                Vector3 targetPos = _lookPoint.transform.position;
                Vector3 flatDirection = targetPos - playerNetworkObject.transform.position;
                flatDirection.y = 0f; // 上下を無視して水平成分だけ
                if (flatDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
                    PlayerManager playerManager = playerNetworkObject.GetComponent<PlayerManager>();
                    playerManager?.SetWarpTarget(playerNetworkObject.transform.position, targetRot);
                }
            }
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new SateliteCannonInteractEffect
            {
                SateliteCannonInteractRPCInvoker = SateliteCannonInteractRPCInvoker,
                _lookPoint = _lookPoint,
            };
        }
    }
}