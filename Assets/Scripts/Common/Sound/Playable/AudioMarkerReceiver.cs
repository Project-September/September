using CRISound;
using UnityEngine;
using UnityEngine.Playables;

namespace September.NewResult
{
    /// <summary>
    /// <see cref="AudioMarker"/>を受け取り、サウンドを再生するコンポーネント
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public class AudioMarkerReceiver : MonoBehaviour, INotificationReceiver
    {
        [SerializeField] private string _cueSheet = "ALLCue";

        private void PlaySound(string cueName)
        {
            CRIAudio.PlaySE(_cueSheet, cueName);
        }

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is AudioMarker marker)
            {
                PlaySound(marker.CueName);
            }
        }
    }
}