using Fusion;
using InGame.Interact;
using UnityEngine;

namespace InGame.Exhibit
{
    // ロンドンテレフォン実装クラス
    public class LondonTelephoneInteractEffect : CharacterInteractEffectBase
    {
        public LondonTelephoneInteractRPCInvoker LondonTelephoneInteractRPC;
        
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            //var player = PlayerRef.FromEncoded(context.Interactor);
            //Debug.Log(player);
            LondonTelephoneInteractRPC.RpcRequestInteraction(PlayerRef.FromEncoded(context.Interactor));
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new LondonTelephoneInteractEffect
            {
                LondonTelephoneInteractRPC = LondonTelephoneInteractRPC,
            };
        }
    }
}