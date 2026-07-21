using System;
using System.Collections.Generic;
using UnityEngine;

namespace InGame.Common.AnimationMontage
{
    [CreateAssetMenu(menuName = "AnimationMontage")]
    public class AnimationMontage : ScriptableObject 
    {
        [SerializeField, HideInInspector] private ulong _id;
        [SerializeField] private AnimationClip _clip;
        [SerializeField] private GameObject _previewModel;
        [SerializeField] private Avatar _overrideAvatar;
        [Header("PlaySettings")]
        [SerializeField, Min(0)] private float _playRate = 1f;
        [SerializeField] private Blend _blendIn;
        [SerializeField] private Blend _blendOut;
        [SerializeField] private bool _loop;
        [SerializeField, Tooltip("")] private AvatarMask _selectedMask;
        [Header("Sections")]
        [SerializeField] private MontageSection[] _sections;
        [Header("Notifies")]
        [SerializeField] private List<NotifyKey> _notifies = new();
        
        public ulong Id => _id;
        public AnimationClip Clip => _clip;
        public GameObject PreviewModel => _previewModel;
        public Avatar OverrideAvatar => _overrideAvatar;
        public float PlayRate => _playRate;
        public Blend BlendIn => _blendIn;
        public Blend BlendOut => _blendOut;
        public bool Loop => _loop || _clip.isLooping;
        public AvatarMask SelectedMask => _selectedMask;
        public MontageSection[] Sections => _sections;
        public List<NotifyKey> Notifies => _notifies;
        
#if UNITY_EDITOR
        public void EditorSetId(ulong value) => _id = value;
#endif
    }
    
    [Serializable]
    public struct Blend
    {
        [SerializeField] private float _blendTime;
        [SerializeField] private AnimationCurve _blendCurve;
        
        public float BlendTime => _blendTime;
        public AnimationCurve BlendCurve => _blendCurve;
    }

    [Serializable]
    public struct MontageSection 
    {
        [SerializeField] private string _name;
        [SerializeField, Min(0)] private float _startTime;
        [SerializeField, Min(0)] private float _endTime;
        
        public string Name => _name;
        public float StartTime => _startTime;
        public float EndTime => _endTime;
    }

    [Serializable]
    public class NotifyKey 
    {
        [SerializeField, Min(0)] private float _time;
        [SerializeReference, SubclassSelector] private INotifyEvent _event;
        [SerializeField] private string _name;

        public float Time { get => _time; set => _time = value; }
        public INotifyEvent Event => _event;
        public string Name => _name;

        public NotifyKey(float time, INotifyEvent @event)
        {
            _time = time;
            _event = @event;
            _name = string.Empty;
        }
    }

    public interface INotifyEvent
    {
        // 実行（ランタイム用）
        void Execute(MontagePlayer montagePlayer);
    }

    /// <summary> root の method 呼べるやつ </summary>
    [Serializable]
    public class CallMethodByNameNotify : INotifyEvent 
    {
        public string _methodName;
        public void Execute(MontagePlayer montagePlayer) {
            if (!string.IsNullOrEmpty(_methodName))
                montagePlayer?.transform.root.gameObject.SendMessage(_methodName, SendMessageOptions.DontRequireReceiver);
        }
    }
}