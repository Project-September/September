using CRISound;
using Fusion;
using September.InGame;
using UnityEngine;
using UnityEngine.Playables;

namespace September.NewResult
{
    /// <summary>
    /// <see cref="AudioMarker"/>を受け取り、サウンドを再生するコンポーネント
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public class AudioMarkerReceiver : NetworkBehaviour, INotificationReceiver
    {
        [SerializeField] private string _cueSheet = "ALLCue";
        [SerializeField] private AudioBroadcaster _audioBroadcaster;

        private void PlaySound(string cueName, SoundTrackingType trackingType)
        {
            if (_audioBroadcaster == null)
            {
                CRIAudio.PlaySE(transform.position, _cueSheet, cueName);
                return;
            }

            _audioBroadcaster.PlaySoundFromCode(cueName, trackingType, Object, Object.InputAuthority);
        }

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is AudioMarker marker)
            {
                PlaySound(marker.CueName, marker.TrackingType);
            }
        }
    }
}
