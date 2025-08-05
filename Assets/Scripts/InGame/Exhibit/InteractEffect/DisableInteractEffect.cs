using System;
using System.Linq;
using Fusion;
using InGame.Interact;
using UnityEngine;

namespace InGame.Exhibit.InteractEffect
{
    [Serializable]
    public class DisableInteractEffect : CharacterInteractEffectBase
    {
        [SerializeField] private float _cooldownTime = 5f;
        private NetworkRunner Runner => NetworkRunner.Instances.FirstOrDefault();
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            var cooldownTime = _cooldownTime;
            target.LastInteractTime = Runner ? Runner.SimulationTime : Time.time;
            target.LastUsedCooldownTime = cooldownTime;
            //何かしらの対応する演出を入れる
        }

        public override CharacterInteractEffectBase Clone()
        {
            return new DisableInteractEffect
            {
                _cooldownTime = _cooldownTime
            };
        }
    }
}
