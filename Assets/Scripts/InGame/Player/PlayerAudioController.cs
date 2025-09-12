using Fusion;
using UnityEngine;
using CRISound;
using September.Common;
using InGame.Common;
using System.Collections.Generic;

namespace September.InGame
{
    [RequireComponent(typeof (AudioBroadcaster))]
    public class PlayerAudioController : NetworkBehaviour
    {
        [SerializeField] private string _sheetName = "ALLCue";
        [SerializeField] private string _footstepCueName = SoundCues.SE.OKB_Footstep.Name; // キャラによって変わる
        [SerializeField] private string _punchSwingCueName = SoundCues.SE.Player_Punch_Swing.Name;
        [SerializeField] private string _punchHitCueName = SoundCues.SE.Player_Punch_Hit.Name;
        [SerializeField] AudioBroadcaster _audioBroadcaster;
        [SerializeField] private AnimationClipPlayer _clipPlayer; // 再生中のアニメーション取得用

        private CRIListenerManager _listenerManager;

        [Header("足音と被らないように停止アニメーションを設定")]
        [SerializeField] private List<AnimationClip> _footstepBlockClipList = new List<AnimationClip>();

        public override void Spawned()
        {
            // 3Dリスナーの設定(カメラ追従)
            if (!Object.HasInputAuthority) return;

            _audioBroadcaster = GetComponent<AudioBroadcaster>();
            _clipPlayer = GetComponentInParent<AnimationClipPlayer>();
            _listenerManager = FindFirstObjectByType<CRIListenerManager>();
            if (_listenerManager == null) return;

            _listenerManager.Attach(Camera.main.transform);
        }

        /// <summary> 足音再生用 Animation Eventから使用 </summary>
        /// <param name="cueName"> キャラ毎の足音のCueName </param>
        public void PlayFootstepSound(AnimationEvent animationEvent)
        {
            if (!HasInputAuthority) return; // 

            string cueName = animationEvent.stringParameter;
            int trackingType = animationEvent.intParameter;

            if (_clipPlayer == null) return;
            // 攻撃モーション中などは鳴らさない
            foreach (var clip in _footstepBlockClipList)
            {
                if (_clipPlayer.IsPlayingTargetClip(clip)) return;
            }

            _audioBroadcaster.PlaySoundFromCode(cueName, trackingType);
        }
    }
}