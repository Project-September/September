using Fusion;
using UnityEngine;
using CRISound;
using September.Common;
using InGame.Common;
using System.Collections.Generic;
using UnityEngine.Playables;
using CriWare;
using Cysharp.Threading.Tasks;
using System.Threading;

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

        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Transform _cameraTransform;
        
        private CRIListenerManager _listenerManager;
        private MoveType _lastDominant = MoveType.Walk; // 揺らぎ防止 初期 Walk
        private float SwitchThreshold = 0.15f;          // 切替に必要な差

        [Header("Debug用")]
        [SerializeField, Range(0f, 5f)] private float _debugBGMVolume = 1f;
        [SerializeField, Range(0f, 5f)] private float _debugSEVolume = 1f;

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
            if (!HasInputAuthority) return;

            _audioBroadcaster = GetComponent<AudioBroadcaster>();
            _clipPlayer = GetComponentInParent<AnimationClipPlayer>();
            _playerTransform = transform.root;
            _cameraTransform = Camera.main.transform;
            SetupListenerAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask SetupListenerAsync(CancellationToken ct)
        {
            CRIListenerManager listenerManager = null;
            await UniTask.WaitUntil(() =>
            {
                listenerManager = CRIListenerManager.GetOrCreateInLocalListener();
                return listenerManager != null;
            }, cancellationToken: ct);

            _listenerManager = listenerManager;

            await UniTask.WaitUntil(() => Camera.main != null, cancellationToken: ct);
            if (ct.IsCancellationRequested) return;

            listenerManager.AttachPlayer(transform.root);        // 一応Meshではなくルートを設定
            listenerManager.AttachCamera(Camera.main.transform); // 音の左右を視界基準に設定
        }

        /// <summary> 足音再生用 Animation Eventから使用 </summary>
        /// <param name="animationEvent">
        /// string     CueName
        /// int        FollowType
        /// float(int) MoveType
        /// </param>
        public void PlayFootstepSound(AnimationEvent animationEvent)
        {
            if (_listenerManager == null) return;

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

            _audioBroadcaster.RPC_PlaySoundFromCode(cueName, SoundTrackingType.Spot, Object.Id);
        }

        // デバッグ用
        // 音実装終わるまで残す
        private void Update()
        {
            // ALLミュート [M]
            if (Input.GetKeyDown(KeyCode.M))
            {
                _debugBGMVolume = 0f;
                _debugSEVolume = 0f;
                CriAtom.SetCategoryVolume("BGM", _debugBGMVolume);
                CriAtom.SetCategoryVolume("SE", _debugSEVolume);
            }

            // BGM音量変更 [V] + [B] + [+ or -]
            // SE音量変更  [V] + [S] + [+ or -]
            if (Input.GetKey(KeyCode.V))
            {
                if (Input.GetKey(KeyCode.B))
                {
                    if (Input.GetKeyDown(KeyCode.Semicolon))
                    {
                        if (_debugBGMVolume >= 5f) return;
                        _debugBGMVolume += 0.5f;
                        CriAtom.SetCategoryVolume("BGM", _debugBGMVolume);
                        Debug.Log($"BGM音量変更：{_debugBGMVolume}");
                    }
                    if (Input.GetKeyDown(KeyCode.Minus))
                    {
                        if (_debugBGMVolume <= 0f) return;
                        _debugBGMVolume -= 0.5f;
                        CriAtom.SetCategoryVolume("BGM", _debugBGMVolume);
                        Debug.Log($"BGM音量変更：{_debugBGMVolume}");
                    }
                }
                if (Input.GetKey(KeyCode.S))
                {
                    if (Input.GetKeyDown(KeyCode.Semicolon))
                    {
                        if (_debugSEVolume >= 5f) return;
                        _debugSEVolume += 0.5f;
                        CriAtom.SetCategoryVolume("SE", _debugSEVolume);
                        Debug.Log($"SE音量変更：{_debugSEVolume}");
                    }
                    if (Input.GetKeyDown(KeyCode.Minus))
                    {
                        if (_debugSEVolume <= 0f) return;
                        _debugSEVolume -= 0.5f;
                        CriAtom.SetCategoryVolume("SE", _debugSEVolume);
                        Debug.Log($"SE音量変更：{_debugSEVolume}");
                    }
                }
            }
        }
    }
}