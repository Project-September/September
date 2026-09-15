using System;
using InGame.Interact;

namespace September.InGame.Tutorial
{
    [Serializable]
    public class TutorialInteractEffect : CharacterInteractEffectBase
    {
        public static event Action<InteractableBase, int> Completed;

        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            // 長押しが成立した時だけ通知する。乗車・移動・カメラ切り替えは行わない。
            Completed?.Invoke(target, context.Interactor);
        }

        public override CharacterInteractEffectBase Clone() => new TutorialInteractEffect();
    }
}
