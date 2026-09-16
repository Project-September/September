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
        }

        public const string AssetName = "SoundVolumeSettings";
        private const string ResourcesPath = AssetName;

        [SerializeField] private List<Entry> _entries = new();

        private static SoundVolumeSettings _instance;
        private Dictionary<string, float> _volumeByCueName;

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
            if (_volumeByCueName == null)
            {
                RebuildLookup();
            }

            return _volumeByCueName.TryGetValue(cueName, out float volume)
                ? Mathf.Max(0f, volume)
                : 1f;
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
            _volumeByCueName = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var entry in _entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.CueName)) continue;
                _volumeByCueName[entry.CueName] = Mathf.Max(0f, entry.Volume);
            }
        }
    }
}
