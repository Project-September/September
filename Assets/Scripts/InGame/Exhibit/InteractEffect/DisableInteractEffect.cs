using System;
using System.Linq;
using Fusion;
using InGame.Interact;
using September.Common;
using UnityEngine;

namespace InGame.Exhibit.InteractEffect
{
    public class DisableInteractEffect : MonoBehaviour
    {
        [SerializeField] private float _cooldownTime = 5f;
        [SerializeField] private InteractableBase _interactable;
        
        NetworkRunner Runner => NetworkRunner.Instances.FirstOrDefault();
        public void OnHitHammerAttack()
        {
            var cooldownTime = _cooldownTime;
            _interactable.LastInteractTime = Runner ? Runner.SimulationTime : Time.time;
            _interactable.LastUsedCooldownTime = cooldownTime;
            //何かしらの対応する演出を入れる
            
        }
    }
}