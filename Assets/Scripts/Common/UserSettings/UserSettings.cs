using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Common.UserSettings
{
    [Serializable]
    public class UserSettings
    {
        public float BGMVolume = 1f;
        public float SEVolume = 1f;
        public float VoiceVolume = 1f;
        public float MouseSensitivity = 1f;
        public float PadSensitivity = 1f;

        private static UserSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }
        
        private static UserSettings _instance;

        private static readonly string FilePath = Application.persistentDataPath + "/UserSettings.json";

        public static void Save(UserSettings settings)
        {
            if (IsFileLocked(FilePath)) return;

            var json = JsonUtility.ToJson(settings, false);
            File.WriteAllText(FilePath, json);
        }

        public static UserSettings Get()
        {
            if(_instance == null)
            {
                return new UserSettings();
            }
            return Instance;
        }

        private static UserSettings Load()
        {
            if (IsFileLocked(FilePath)) return null;

            var userSettings = new UserSettings();
            if (File.Exists(FilePath))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), userSettings);
            }

            return userSettings;
        }
        private static bool IsFileLocked(string path)
        {
            if (!File.Exists(path)) return false;

            try
            {
                using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return false; // 開けた = ロックされてない
                }
            }
            catch (IOException)
            {
                return true; // 開けない = 誰かが使ってる
            }
        }
    }
}