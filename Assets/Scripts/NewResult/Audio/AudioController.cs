using System;
using CRISound;
using UnityEngine;
using UnityEngine.Playables;

namespace September.NewResult
{
    public class AudioController : MonoBehaviour, INotificationReceiver
    {
        [SerializeField] private Transform _transform;
        [SerializeField] private string _cueSheet = "ALLCue";

        private void Start()
        {
            if (_transform == null) _transform = transform;
        }

        private void PlaySound(string cueName)
        {
            CRIAudio.PlaySE(_transform.position, _cueSheet, cueName);
        }

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is AudioMarker marker)
            {
                Debug.Log(marker.CueName);
                PlaySound(marker.CueName);
            }
        }
    }
}