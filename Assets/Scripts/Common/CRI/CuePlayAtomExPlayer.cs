using System;
using System.Collections.Generic;
using System.Linq;
using static CriWare.CriAtomEx;
using CriWare;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CRISound
{
    public enum SoundType
    {
        BGM,
        SE,
        Voice
    }

    public enum SoundTrackingType
    {
        Spot,
        Follow
    }
    public class CuePlayAtomExPlayer
    {
        static CuePlayAtomExPlayer _instance = new();
        public static CuePlayAtomExPlayer Instance => _instance;

        private const int SoundTypeCount = 3;
        private SoundPlayer[] _soundPlayer = new SoundPlayer[SoundTypeCount];

        private SoundPlayer _bgmPlayer;
        private SEPlayerWith3D _sePlayer;
        private SoundPlayer _voicePlayer;

        private CuePlayAtomExPlayer()
        {
            _soundPlayer[(int)SoundType.BGM] = _bgmPlayer = new SoundPlayer(SoundType.BGM);
            _soundPlayer[(int)SoundType.SE] = _sePlayer = new SEPlayerWith3D();
            _soundPlayer[(int)SoundType.Voice] = _voicePlayer = new SoundPlayer(SoundType.Voice);
        }

        public static SEPlayerWith3D SE => _instance._sePlayer;

        private CueInfo[] _cueInfoList;
        private CriAtomExPlayer _atomExPlayer;
        private CriAtomExAcb _atomExAcb;

        private const int AtomSourceBuffer = 10;

        private bool _isReady = false;
        private readonly Dictionary<string, SoundDic> _soundDic = new();
        private List<Tuple<SoundType, string, string>> _defaultSoundList = new();

        public bool IsReady => _isReady;

        public static void Initialize()
        {
            _instance.LoadCueSheet();
        }

        private void OnDestroy()
        {
            foreach (var player in _soundPlayer)
            {
                player.Dispose();
            }
        }

        public SoundPlayer Player(SoundType soundType)
        {
            return _soundPlayer[(int)soundType];
        }
        
        private async void LoadCueSheet()
        {
            // CRIAtomの処理
            var criAtom = Object.FindObjectOfType<CriAtom>();
            // CRIが見つからなければ動的に生成する
            if (criAtom == null)
            {
                _isReady = false;
                // CRIのロード(あとでAddressableに変更)
                var obj = Resources.Load<GameObject>("CRIObject");
                Object.Instantiate(obj);
                criAtom = Object.FindObjectOfType<CriAtom>();
            }

            if (_isReady)
                return;

            // cueシートファイルのロード待ち
            await UniTask.WaitUntil(() => criAtom.cueSheets.All(cs => cs.IsLoading == false));

            // cue情報の取得
            foreach (var sheet in criAtom.cueSheets)
            {
                _soundDic.Add(sheet.name, new SoundDic(sheet.acb));
            }

            _isReady = true;

            foreach (var player in _soundPlayer)
            {
                player.SetUp();
                player.SetVolume(1.0f);
            }

            foreach (var s in _defaultSoundList)
            {
                _soundPlayer[(int)s.Item1].Play(s.Item2, s.Item3);
            }

            _defaultSoundList.Clear();
        }

        public void PlayQueue(SoundType type, string acb, string name)
        {
            _instance._defaultSoundList.Add(new Tuple<SoundType, string, string>(type, acb, name));
        }

        public void ResetCategoryVolume()
        {
            CriAtom.SetCategoryVolume("BGM", 1.0f);
            CriAtom.SetCategoryVolume("SE", 1.0f);
            //CriAtom.SetCategoryVolume("Voice", 1.0f);
        }

        private class SoundDic
        {
            private CriAtomExAcb _atomExAcb;
            private Dictionary<string, CueInfo> _cueInfoDic = new();

            public SoundDic(CriAtomExAcb acb)
            {
                _atomExAcb = acb;
                foreach (var cueInfo in acb.GetCueInfoList())
                {
                    _cueInfoDic.Add(cueInfo.name, cueInfo);
                }
            }

            public CriAtomExAcb GetAcb()
            {
                return _atomExAcb;
            }

            public CueInfo GetCueInfo(string cueName)
            {
                return _cueInfoDic[cueName];
            }
        }

        public class SoundPlayer
        {
            private SoundType _type;
            private string _currentCueName;
            private float _volume = 1.0f;
            protected CriAtomExPlayer _atomExPlayer;

            public bool IsPlaying => _atomExPlayer.GetStatus() == CriAtomExPlayer.Status.Playing;
            public string CurrentCueName => _currentCueName;

            public CriAtomExPlayer Player => _atomExPlayer;

            public SoundPlayer(SoundType type)
            {
                _type = type;
            }

            public virtual void SetUp()
            {
                _atomExPlayer = new CriAtomExPlayer();
            }

            public virtual void Dispose()
            {
                _atomExPlayer.Dispose();
            }

            public virtual void SetVolume(float volume)
            {
                _volume = volume;
                _atomExPlayer.SetVolume(_volume);
            }

            public bool IsPlayingCue(string cueName)
            {
                return _atomExPlayer.GetStatus() == CriAtomExPlayer.Status.Playing &&
                       _currentCueName == cueName;
            }

            public virtual CriAtomExPlayback Play(string cueSheet, string cueName, float delay = 0.0f)
            {
                _currentCueName = cueName;

                if (!_instance.IsReady)
                {
                    Debug.LogWarning($"[SoundPlayer:Queue] Not ready. Queued: {_type}, {cueSheet}/{cueName}");
                    _instance.PlayQueue(_type, cueSheet, cueName);
                    return default;
                }

                CueInfo info = _instance._soundDic[cueSheet].GetCueInfo(cueName);
                _atomExPlayer.SetCue(_instance._soundDic[cueSheet].GetAcb(), info.id);
                _atomExPlayer.SetPreDelayTime(delay);

                var playback = _atomExPlayer.Start();
                
                var cueId = info.id;
                _atomExPlayer.SetCue(_instance._soundDic[cueSheet].GetAcb(), cueId);

                return playback;
            }

            /// <summary> 2D用SEPlayerを止める </summary>
            public virtual void Stop()
            {
                _atomExPlayer.Stop();
            }

            /// <summary> 指定した名前のキューが再生されていたら止める(2D) </summary>
            /// <param name="cueName"></param>
            public void StopSEFromCueName(string cueName)
            {
                if (IsPlayingCue(cueName))
                //if (_currentCueName == cueName)
                {
                    _atomExPlayer.Stop();
                }
            }
        }

        public class SEPlayerWith3D : SoundPlayer
        {
            /// <summary>3Dサウンド再生用(単一)</summary>
            public class Sound3D
            {
                protected CriAtomEx3dSource _criAtomEx3DSource = new();
                protected CriAtomExPlayer _atomExPlayer3D = new();
                public CriAtomEx3dSource CroAtomEx3DSource => _criAtomEx3DSource;
                public CriAtomExPlayer CriAtomExPlayer3D => _atomExPlayer3D;

                protected CriAtomExPlayback _criAtomExPlayback3D;
                public CriAtomExPlayback CriAtomExPlayback3D => _criAtomExPlayback3D;
                private string _currentCueName;

                public bool IsBusy => _atomExPlayer3D.GetStatus() == CriAtomExPlayer.Status.Playing;
                public string CurrentCueName => _currentCueName;

                public void Dispose()
                {
                    _atomExPlayer3D.Dispose();
                    _criAtomEx3DSource.Dispose();
                }

                public void Play3D(Vector3 playPos, string cueSheet, string cueName)
                {
                    _currentCueName = cueName;
                    _criAtomEx3DSource.SetPosition(playPos.x, playPos.y, playPos.z);
                    _criAtomEx3DSource.Update();

                    CueInfo info = _instance._soundDic[cueSheet].GetCueInfo(cueName);
                    _atomExPlayer3D.SetCue(_instance._soundDic[cueSheet].GetAcb(), info.id);
                    _atomExPlayer3D.SetPanType(CriAtomEx.PanType.Pos3d);
                    _atomExPlayer3D.Set3dSource(_criAtomEx3DSource);
                    _atomExPlayer3D.Set3dListener(_instance._sePlayer.Listener);
                    _atomExPlayer3D.UpdateAll();
                    _criAtomExPlayback3D = _atomExPlayer3D.Start();
                }

                public bool IsPlayingCue(string cueName)
                {
                    var status = _atomExPlayer3D.GetStatus();
                    var isNameMatch = _currentCueName == cueName;
                    return status == CriAtomExPlayer.Status.Playing && isNameMatch;
                }

                /// <summary>
                /// 3DSourceの位置セットと更新
                /// 移動する音の場合、Update等で呼び出しておく必要がある
                /// </summary>
                /// <param name="pos"></param>
                public void UpdateSourcePosition(Vector3 pos)
                {
                    CroAtomEx3DSource.SetPosition(pos.x, pos.y, pos.z);
                    CroAtomEx3DSource.Update();
                }

                public CriAtomEx3dSource GetSource() { return CroAtomEx3DSource; }
            }


            private CriAtomEx3dListener _listener;
            public CriAtomEx3dListener Listener => _listener;
            Sound3D[] _sound3Ds = new Sound3D[AtomSourceBuffer];

            public SEPlayerWith3D() : base(SoundType.SE)
            {
            }

            public override void SetUp()
            {
                _listener = new CriAtomEx3dListener();
                _atomExPlayer = new CriAtomExPlayer();
                for (int i = 0; i < AtomSourceBuffer; ++i)
                {
                    _sound3Ds[i] = new Sound3D();
                }
            }

            public override void Dispose()
            {
                base.Dispose();
                for (int i = 0; i < AtomSourceBuffer; ++i)
                {
                    _sound3Ds[i].Dispose();
                }
            }

            private Sound3D GetPlayer()
            {
                for (int i = 0; i < AtomSourceBuffer; ++i)
                {
                    if (_sound3Ds[i].IsBusy) continue;
                    return _sound3Ds[i];
                }

                return null;
            }

            // 指定したSEの再生が終了しているかどうか
            // 指定した名前のキューが一つでも再生中であれば true
            public bool Is3DCuePlaying(string cueName)
            {
                foreach (var s in _sound3Ds)
                {
                    if (s.IsPlayingCue(cueName))
                    {
                        Debug.Log("Playing cue");
                        return true;
                    }
                        
                }

                return false;
            }

            // 指定したSEが再生中か
            // 指定した再生元が再生中であれば true 
            public bool Is3DCuePlayingPlaybackOrigin(CriAtomExPlayback playback)
            {
                return playback.GetStatus() == CriAtomExPlayback.Status.Playing;
            }

            // 指定したSEの再生が終了しているか
            // 指定した再生元が再生中であれば false
            public bool Is3DCueStoppedPlaybackOrigin(CriAtomExPlayback playback)
            {
                return playback.GetStatus() == CriAtomExPlayback.Status.Removed;
            }

            // すべてのSEの再生が終了しているか確認したいとき
            public bool IsAny3DPlaying()
            {
                return _sound3Ds.Any(s => s.IsBusy);
            }

            public Sound3D Play3D(Vector3 playPos, string cueSheet, string cueName)
            {
                Sound3D player = GetPlayer();
                if (player == null)
                {
                    Debug.LogWarning("3D音声の再生上限です");
                    return null;
                }

                player.Play3D(playPos, cueSheet, cueName);
                return player;
            }

            /// <summary> 指定した名前のキューが再生されていたら止める(3D) </summary>
            /// <param name="cueName"></param>
            public void Stop3DSEFromCueName(string cueName)
            {
                foreach (var s in _sound3Ds)
                {
                    if (Is3DCuePlaying(cueName))
                    {
                        s.CriAtomExPlayer3D.Stop();
                        return;
                    }
                }
            }

            /// <summary> 全ての3D再生を止める </summary>
            public void Stop3DSEAll()
            {
                foreach (var s in _sound3Ds)
                {
                    s.CriAtomExPlayer3D.Stop();
                }
            }
        }
    }
}