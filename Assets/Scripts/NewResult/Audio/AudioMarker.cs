using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace September.NewResult
{
    public class AudioMarker : Marker, INotification
    {
        [SerializeField] private string _cueName;
        
        public string CueName => _cueName;
        public PropertyName id { get; }
    }
}