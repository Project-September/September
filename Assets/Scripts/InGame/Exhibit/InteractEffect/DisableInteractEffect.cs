using System;
using System.Linq;
using CRISound;
using Cysharp.Threading.Tasks;
using Fusion;
using InGame.Interact;
using Result;
using September.Common;
using September.InGame;
using UnityEngine;

namespace InGame.Exhibit.InteractEffect
{
    public class DisableInteractEffect : MonoBehaviour
    {
        [SerializeField] private float _cooldownTime = 5f;
        [SerializeField] private InteractableBase _interactable;
        private ExhibitType  _exhibitType;
        private static readonly string CueName = "SE_Hulk_Break";
        private AudioBroadcaster _audio;
        NetworkRunner Runner => NetworkRunner.Instances.FirstOrDefault();

        public ExhibitType ExhibitType => _exhibitType;

        private void Start()
        {
            _exhibitType = gameObject.GetComponent<InteractableBase>().ExhibitType;
            _audio = GetComponent<AudioBroadcaster>();
        }

        public void OnHitHammerAttack(PlayerRef actor)
        {
            var cooldownTime = _cooldownTime;
            _interactable.LastInteractTime = Runner ? Runner.SimulationTime : Time.time;
            _interactable.LastUsedCooldownTime = cooldownTime;
            //何かしらの対応する演出を入れる
            _interactable.PlayCooldownEffect(cooldownTime).Forget();
            _audio.RPC_PlaySoundFromCode(CueName,SoundTrackingType.Spot,default,actor);
        }
    }
}