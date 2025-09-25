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
            SateliteCannonInteractRPCInvoker.Rpc_RequestInteraction(playerRef, _lookPoint.position);
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