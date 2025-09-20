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

        /// <summary> 名前が一致する音どれか一つ </summary>
        /// <param name="cueName"></param>
        /// <returns> 再生中 = true </returns>
        public static bool IsSePlaying(string cueName) =>
            CuePlayAtomExPlayer.SE.Is3DCuePlaying(cueName);

        /// <summary> playbackから指定する特定の音 </summary>
        /// <param name="playback"></param>
        /// <returns> 再生中 = true </returns>
        public static bool IsSePlayingPlaybackOrigin(CriAtomExPlayback playback) =>
            CuePlayAtomExPlayer.SE.Is3DCuePlayingPlaybackOrigin(playback);
    }
}