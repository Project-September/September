using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace September.NewResult
{
    /// <summary>
    /// CRIのキュー名を指定してサウンドの再生を要求するマーカー。
    /// タイムラインなどにマーカーを作成できます。
    /// 再生には<see cref="AudioMarkerReceiver"/>コンポーネントが必要です。
    /// </summary>
    public class AudioMarker : Marker, INotification
    {
        [SerializeField] private string _cueName;
        
        public string CueName => _cueName;
        public PropertyName id { get; }
    }
}