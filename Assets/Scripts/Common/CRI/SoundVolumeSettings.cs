using System;
using System.Collections.Generic;
using UnityEngine;

namespace CRISound
{
    [CreateAssetMenu(fileName = AssetName, menuName = "CRI/Sound Volume Settings")]
    public sealed class SoundVolumeSettings : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string CueName;
            [Min(0f)] public float Volume = 1f;
            [Tooltip("3D再生時の距離減衰をUnity側で上書きします。無効の場合はCRI側の設定を使用します。")]
            public bool Override3DDistance;
            [Tooltip("この距離までは距離による音量減衰を行いません。")]
            [Min(0f)] public float MinDistance = 1f;
            [Tooltip("CRIの距離減衰で最小音量になる距離です。Min Distanceより大きく設定してください。")]
            [Min(0.01f)] public float MaxDistance = 20f;
        }

        public const string AssetName = "SoundVolumeSettings";
        private const string ResourcesPath = AssetName;

        [SerializeField] private List<Entry> _entries = new();

        private static SoundVolumeSettings _instance;
        private Dictionary<string, Entry> _entryByCueName;

        public static float GetVolume(string cueName)
        {
            if (_instance == null)
            {
                _instance = Resources.Load<SoundVolumeSettings>(ResourcesPath);
            }

            return _instance != null ? _instance.GetVolumeInternal(cueName) : 1f;
        }

        private float GetVolumeInternal(string cueName)
        {
            return TryGetEntry(cueName, out var entry) ? Mathf.Max(0f, entry.Volume) : 1f;
        }

        public static bool TryGet3DDistance(string cueName, out float minDistance, out float maxDistance)
        {
            minDistance = 0f;
            maxDistance = 0f;
            if (_instance == null)
            {
                _instance = Resources.Load<SoundVolumeSettings>(ResourcesPath);
            }

            if (_instance == null || !_instance.TryGetEntry(cueName, out var entry) ||
                !entry.Override3DDistance) return false;

            minDistance = Mathf.Max(0f, entry.MinDistance);
            maxDistance = Mathf.Max(minDistance + 0.01f, entry.MaxDistance);
            return true;
        }

        private bool TryGetEntry(string cueName, out Entry entry)
        {
            if (_entryByCueName == null) RebuildLookup();
            entry = null;
            return !string.IsNullOrEmpty(cueName) && _entryByCueName.TryGetValue(cueName, out entry);
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        private void RebuildLookup()
        {
            _entryByCueName = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var entry in _entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.CueName)) continue;
                entry.Volume = Mathf.Max(0f, entry.Volume);
                entry.MinDistance = Mathf.Max(0f, entry.MinDistance);
                entry.MaxDistance = Mathf.Max(entry.MinDistance + 0.01f, entry.MaxDistance);
                _entryByCueName[entry.CueName] = entry;
            }
        }
    }
}
