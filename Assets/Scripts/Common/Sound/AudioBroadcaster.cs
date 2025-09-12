using CRISound;
using CriWare;
using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace September.InGame
{
    /// <summary>
    /// オブジェクトの音を 自分→2D、その他→3D で鳴らすためのコンポーネント
    /// Animation Eventからも使用可能なので、AnimatorControllerが付いている階層に付ける
    /// </summary>
    public class AudioBroadcaster : NetworkBehaviour
    {
        // 位置更新用サウンドデータ
        class FollowEntry
        {
            public Transform Target { get; }
            public CuePlayAtomExPlayer.SEPlayerWith3D.Sound3D SEPlayer { get; }
            public CriAtomEx3dSource Source { get; }
            public CriAtomExPlayback Playback { get; }

            public FollowEntry(Transform target, CuePlayAtomExPlayer.SEPlayerWith3D.Sound3D sePlayer)
            {
                Target   = target;
                SEPlayer = sePlayer;
                Source   = sePlayer.CroAtomEx3DSource;
                Playback = sePlayer.CriAtomExPlayback3D;
            }
        }

        [SerializeField] string _cueSheet = "ALLCue";

        private List<FollowEntry> _followingList = new List<FollowEntry>(); // 移動しながら鳴る音のリスト

        /// <summary>
        /// アニメーションイベントから呼び出す足音などの再生
        /// 音源の位置はAudioBroadcasterが付いた自身のTransform固定
        /// ローカル→2D再生、その他→3D再生
        /// </summary>
        /// <param name="animationEvent"></param>
        public void PlaySoundFromAnimationEvent(AnimationEvent animationEvent)
        {
            if (!HasInputAuthority) return;

            string cueName = animationEvent.stringParameter;
            int trackingType = animationEvent.intParameter;

            CRIAudio.PlaySE(_cueSheet, cueName);                                                // 2D再生
            RPC_Request3DSound(Object.Id, _cueSheet, cueName, (SoundTrackingType)trackingType); // 3D再生依頼
        }

        /// <summary>
        /// スクリプトから直接呼び出す再生
        /// 音源の位置となるオブジェクトを指定可能
        /// ローカル→2D再生、その他→3D再生
        /// </summary>
        /// <param name="cueName"></param>
        /// <param name="trackingType">短い音→ Spot(0)、移動しながら鳴る音→ Follow(1)</param>
        /// <param name="sourceObjId"> 発声元のオブジェクト </param>
        public void PlaySoundFromCode(string cueName, int trackingType, NetworkId sourceObjId = default)
        {
            if (!HasInputAuthority) return;

            // ID入力がない(デフォルト)の場合は自分自身を使用 (AnimationEvent用)
            if (!sourceObjId.IsValid && Object)
            {
                sourceObjId = Object.Id;
            }

            CRIAudio.PlaySE(_cueSheet, cueName);                                                  // 2D再生
            RPC_Request3DSound(sourceObjId, _cueSheet, cueName, (SoundTrackingType)trackingType); // 3D再生依頼
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_Request3DSound(NetworkId targetId, string sheet, string cue, SoundTrackingType tracking)
        {
            // サーバーから All へ配布（ここは Host/Server だけが実行）
            RPC_Play3DSound(targetId, sheet, cue, tracking);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_Play3DSound(NetworkId targetId, string sheetName, string cueName, SoundTrackingType trackingType)
        {
            if (HasInputAuthority) return; // 操作中の場合は3D再生しない(二重再生防止)

            if (Runner.TryFindObject(targetId, out var networkObj))
            {
                Transform followTransform = networkObj.transform;


                if (trackingType == SoundTrackingType.Spot)
                {
                    CRIAudio.PlaySE(followTransform.position, sheetName, cueName);                // 3D再生
                }
                else if (trackingType == SoundTrackingType.Follow)
                {
                    var sePlayer = CRIAudio.PlaySE(followTransform.position, sheetName, cueName); // 3D再生
                    var followSound = new FollowEntry(followTransform, sePlayer);
                    _followingList.Add(followSound);                                              // 追跡リストに追加、LateUpdateで位置更新
                    Debug.Log($"追跡中の音数:{_followingList.Count}");
                }
            }
            else
            {
                Debug.LogWarning("SE再生位置に設定されているオブジェクトが見つかりません");
                return;
            }

        }

        void LateUpdate()
        {
            // 移動しながら鳴る音用
            for (int i = _followingList.Count - 1; i >= 0; i--)
            {
                var sound = _followingList[i];
                if (sound.Target == null || CRIAudio.IsSePlayingPlaybackOrigin(sound.Playback) == false)
                {
                    _followingList.RemoveAt(i);
                    continue;
                }
                var pos = sound.Target.position;
                sound.SEPlayer.UpdateSourcePosition(pos);
            }
        }
    }
}