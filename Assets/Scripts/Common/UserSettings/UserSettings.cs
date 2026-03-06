using System;
using System.IO;
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
            var json = JsonUtility.ToJson(settings, false);
            File.WriteAllText(FilePath, json);
        }

        public static UserSettings Get()
        {
            return Instance;
        }

        private static UserSettings Load()
        {
            var userSettings = new UserSettings();
            if (File.Exists(FilePath))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), userSettings);
            }

            return userSettings;
        }
    }
}