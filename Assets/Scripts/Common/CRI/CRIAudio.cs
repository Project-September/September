using CriWare;
using UnityEngine;

namespace CRISound
{
    public static class CRIAudio
    {
        // 再生、中止用
        public static void PlayBGM(string cueSheet, string cueName) =>
            CuePlayAtomExPlayer.Instance.Player(SoundType.BGM).Play(cueSheet, cueName);

        public static void StopBGM(string cueSheet, string cueName) =>
            CuePlayAtomExPlayer.Instance.Player(SoundType.BGM).Stop();

        /// <summary> 2D用SEPlayerを止める </summary>
        public static void StopSE() =>
            CuePlayAtomExPlayer.Instance.Player(SoundType.SE).Stop();

        /// <summary> 指定した名前の2Dサウンドを止める </summary>
        /// <param name="cueName"></param>
        public static void StopSEFromCueName(string cueName) =>
            CuePlayAtomExPlayer.Instance.Player(SoundType.SE).StopSEFromCueName(cueName);

        /// <summary> 指定した名前の3Dサウンドを止める </summary>
        /// <param name="cueName"></param>
        public static void Stop3DSEFromCueName(string cueName) =>
            CuePlayAtomExPlayer.SE.Stop3DSEFromCueName(cueName);

        public static void PlaySE(string cueSheet, string cueName) =>
        CuePlayAtomExPlayer.Instance.Player(SoundType.SE).Play(cueSheet, cueName);
        
        public static void PlayVoice(string cueSheet, string cueName) =>
        CuePlayAtomExPlayer.Instance.Player(SoundType.Voice).Play(cueSheet, cueName);

        public static CuePlayAtomExPlayer.SEPlayerWith3D.Sound3D PlaySE(Vector3 pos, string cueSheet, string cueName) =>
        CuePlayAtomExPlayer.SE.Play3D(pos, cueSheet, cueName);


        // 指定したサウンドが再生中なのかを判定する
        public static bool IsVoicePlaying(string cueName) =>
            CuePlayAtomExPlayer.Instance.Player(SoundType.Voice).IsPlayingCue(cueName);

        public static bool IsBGMPlaying(string cueName) =>
            CuePlayAtomExPlayer.Instance.Player(SoundType.BGM).IsPlayingCue(cueName);

        /// <summary> 名前が一致するサウンドどれか一つ </summary>
        /// <param name="cueName"></param>
        /// <returns> 再生中 = true </returns>
        public static bool IsSePlaying(string cueName) =>
            CuePlayAtomExPlayer.SE.Is3DCuePlaying(cueName);

        /// <summary> playbackから指定する特定のサウンドが再生中か </summary>
        /// <param name="playback"></param>
        /// <returns> 再生中 = true </returns>
        public static bool IsSePlayingPlaybackOrigin(CriAtomExPlayback playback) =>
            CuePlayAtomExPlayer.SE.Is3DCuePlayingPlaybackOrigin(playback);

        /// <summary> playbackから指定する特定のサウンドが鳴り終わっているか </summary>
        /// <returns> 終了 = true </returns>
        public static bool IsSeStoppedPlaybackOrigin(CriAtomExPlayback playback) =>
            CuePlayAtomExPlayer.SE.Is3DCueStoppedPlaybackOrigin(playback);
    }
}