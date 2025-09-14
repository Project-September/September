using Fusion;
using UnityEngine;
using CRISound;
using September.Common;
using InGame.Common;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace September.InGame
{
    [RequireComponent(typeof (AudioBroadcaster))]
    public class PlayerAudioController : NetworkBehaviour
    {
        // 現在AnimationClipPlayerと連携してないため値変更の可能性がある…
        enum MoveType
        {
            Walk = 1,
            Run = 2
        }

        [SerializeField] private string _sheetName = "ALLCue";
        [SerializeField] private string _footstepCueName = SoundCues.SE.OKB_Footstep.Name; // キャラによって変わる
        [SerializeField] private string _punchSwingCueName = SoundCues.SE.Player_Punch_Swing.Name;
        [SerializeField] private string _punchHitCueName = SoundCues.SE.Player_Punch_Hit.Name;
        [SerializeField] AudioBroadcaster _audioBroadcaster;
        [SerializeField] private AnimationClipPlayer _clipPlayer; // 再生中のアニメーション取得用

        [Header("足音と被らないように停止アニメーションを設定")]
        [SerializeField] private List<AnimationClip> _footstepBlockClipList = new List<AnimationClip>();

        private CRIListenerManager _listenerManager;
        private MoveType _lastDominant = MoveType.Walk; // 揺らぎ防止 初期 Walk
        private float SwitchThreshold = 0.15f;          // 切替に必要な差

        // 現在の優勢クリップを判定 Walk or Run
        private MoveType GetDominantAnimation()
        {
            float weightWalk = _clipPlayer.BaseMixer.GetInputWeight((int)MoveType.Walk);
            float weightRun = _clipPlayer.BaseMixer.GetInputWeight((int)MoveType.Run);

            if (_lastDominant == MoveType.Walk)
            {
                if (weightRun - weightWalk > SwitchThreshold)
                {
                    _lastDominant = MoveType.Run;
                }
            }
            else
            {
                if (weightWalk -  weightRun > SwitchThreshold)
                {
                    _lastDominant = MoveType.Walk;
                }
            }

            return _lastDominant;
        }

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
        /// <param name="animationEvent">
        /// string     CueName
        /// int        FollowType
        /// float(int) MoveType
        /// </param>
        public void PlayFootstepSound(AnimationEvent animationEvent)
        {
            // 自分からでなければ鳴らさない
            if (!HasInputAuthority) return;

            if (_clipPlayer == null) return;
            // 攻撃モーション中などは鳴らさない
            foreach (var clip in _footstepBlockClipList)
            {
                if (_clipPlayer.IsPlayingTargetClip(clip)) return;
            }

            string cueName = animationEvent.stringParameter;
            int trackingType = animationEvent.intParameter;
            int speedType = (int)animationEvent.floatParameter;
            int domMoveType = (int)GetDominantAnimation();

            // AnimationEvenに設定された Walk or Run の値と現在の優勢クリップが異なっていたら鳴らさない
            if (speedType != domMoveType) return; // walk = 1, run = 2

            _audioBroadcaster.PlaySoundFromCode(cueName, trackingType);
        }
    }
}