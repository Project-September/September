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
    public class AudioBroadcaster : NetworkBehaviour //ImtStateMachine<InGameManager>.State
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
        /// アニメーションイベントから呼び出す呼び出す音の再生(展示物用)
        /// 音源の位置はAudioBroadcasterが付いた自身のTransform固定
        /// 3D再生
        /// </summary>
        /// <param name="animationEvent"></param>
        public void PlaySoundFromAnimationEventForExhibit(AnimationEvent animationEvent)
        {
            string cueName = animationEvent.stringParameter;
            int trackingType = animationEvent.intParameter;

            RPC_PlaySoundFromCode(cueName, SoundTrackingType.Spot, Object.Id);
        }

        /// <summary>
        /// アニメーションイベントから呼び出す音の再生(プレイヤー用)
        /// 音源の位置はAudioBroadcasterが付いた自身のTransform固定
        /// ローカル→2D再生、その他→3D再生
        /// </summary>
        /// <param name="animationEvent"></param>
        public void PlaySoundFromAnimationEventForPlayer(AnimationEvent animationEvent)
        {
            // 操作中のプレイヤーではないときは何もしない
            if (!HasInputAuthority) return;

            string cueName = animationEvent.stringParameter;
            int trackingType = animationEvent.intParameter;

            RPC_PlaySoundFromCode(cueName, SoundTrackingType.Spot, Object.Id);
        }

        /// <summary>
        /// Spot固定の音再生
        /// NetworkObject以外のポジション指定用(Vector3)
        /// 3D再生
        /// </summary>
        /// <param name="cueName"></param>
        /// <param name="worldPos"></param>
        public void PlaySoundAtPosition(string cueName, Vector3 worldPos)
        {
            if (!HasStateAuthority) return;
            RPC_Request3DSoundAtPosition(worldPos, _cueSheet, cueName);
        }

        /// <summary>
        /// Spot固定の音再生
        /// NetworkObject以外のポジション指定用(Transform)
        /// 3D再生
        /// </summary>
        /// <param name="cueName"></param>
        /// <param name="transform"></param>
        public void PlaySoundAtTransform(string cueName, Transform transform)
        {
            if (!transform) return;
            PlaySoundAtPosition(cueName, transform.position);
        }

        //-------------------------------------------------------//
        /// <summary>
        /// スクリプトから直接呼び出す再生
        /// 音源の位置となるオブジェクトを指定可能(指定する場合は NetworkObject or NetworkId)
        /// ローカル→2D再生、その他→3D再生
        /// </summary>
        /// <param name="cueName"> キューの名前 </param>
        /// <param name="trackingType"> 短い音→ Spot(0)、移動しながら鳴る音→ Follow(1) </param>
        /// <param name="sourceObjId"> 発声元のオブジェクト NetworkId </param>
        /// <param name="playerRef"> 2D再生対象のプレイヤー </param>
        /// <param name="networkObj"> 発声元のオブジェクト NetworkObject </param>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_PlaySoundFromCode(string cueName, SoundTrackingType trackingType = default, NetworkId sourceObjId = default, PlayerRef playerRef = default, NetworkObject networkObj = default)
        {
            // 2D再生用のプレイヤーも音源もわからない場合 → 3D再生のみ
            if (playerRef.IsNone && (!sourceObjId.IsValid && Object))
            {
                sourceObjId = Object; // 音源はこれを呼んだオブシェクトに
                Play3DSound(sourceObjId, cueName, trackingType);
            }
            // プレイヤー指定がないが音源がわかる // 3D再生 音源が自分のときは+2D再生
            else if ((playerRef.IsNone && sourceObjId.IsValid) || (playerRef.IsNone && networkObj.IsValid))
            {
                if ((sourceObjId == Object || networkObj == Object) && HasInputAuthority) // 音源が自分だったら
                {
                    playerRef = Runner.LocalPlayer;
                    CheckAndPlay2DSound(playerRef, cueName, trackingType); // 2D再生 関数内で該当者以外return
                }
                else
                {
                    Play3DSound(sourceObjId, cueName, trackingType);
                }
                    
            }
            // プレイヤー指定がある 音源あり or なし
            else if (!playerRef.IsNone)
            {
                sourceObjId = Object;
                if (playerRef == Runner.LocalPlayer)
                {
                    CheckAndPlay2DSound(playerRef, cueName, trackingType); // 2D再生 関数内で該当者以外return
                }
                else
                {
                    Play3DSound(sourceObjId, cueName, trackingType); //rpc?
                }
            }
        }

        private void CheckAndPlay2DSound(PlayerRef playerRef, string cueName, SoundTrackingType trackingType, RpcInfo info = default)
        {
            // プレイヤー指定があるが入力対象じゃない場合
            if (!playerRef.IsNone && !HasInputAuthority)
            {
                if (playerRef == Runner.LocalPlayer)
                {
                    CRIAudio.PlaySE(_cueSheet, cueName);
                    return;
                }
                else
                {
                    return;
                }
            }
            // プレイヤー指定があって入力対象だった場合
            else if (!playerRef.IsNone && HasInputAuthority)
            {
                CRIAudio.PlaySE(_cueSheet, cueName);
                return;
            }
            // プレイヤー指定がないが音源が分かる // パターン2のとき
            else if (playerRef.IsNone)
            {
                CRIAudio.PlaySE(_cueSheet, cueName);
                return;
            }
            // パターン3のとき
            else if (!playerRef.IsNone && playerRef == Runner.LocalPlayer)
            {
                CRIAudio.PlaySE(_cueSheet, cueName);
                return;
            }
        }

        private void Play3DSound(NetworkId targetId, string cueName, SoundTrackingType trackingType = default, RpcInfo info = default)
        {
            if (info.IsInvokeLocal) return;
            if (Runner.TryFindObject(targetId, out var networkObj))
            {
                Transform followTramsform = networkObj.transform;

                if (trackingType == SoundTrackingType.Spot)
                {
                    CRIAudio.PlaySE(followTramsform.position, _cueSheet, cueName);                // 3D再生
                }
                else if (trackingType == SoundTrackingType.Follow)
                {
                    var sePlayer = CRIAudio.PlaySE(followTramsform.position, _cueSheet, cueName); // 3D再生
                    var followSound = new FollowEntry(followTramsform, sePlayer);
                    _followingList.Add(followSound);                                              // 追跡リストに追加、LateUpdateで位置更新
                }
            }
            else
            {
                Debug.LogWarning("SE再生位置に設定されているオブジェクトが見つかりません");
                return;
            }
        }
        //-------------------------------------------------------//

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_Request3DSoundAtPosition(Vector3 worldPos, string sheetName, string cueName)
        {
            CRIAudio.PlaySE(worldPos, sheetName, cueName);
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