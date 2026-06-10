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

        private static int _maxWaitMs = 5000;
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

        public static async UniTask Save(UserSettings settings)
        {
            int waited = 0;
            const int interval = 50;

            while (IsFileLocked(FilePath))
            {
                if (waited >= _maxWaitMs)
                {
                    Debug.LogWarning("Save skipped: file lock timeout");
                    return;
                }

                await UniTask.Delay(interval);
                waited += interval;
            }

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
                try
                {
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), userSettings);
                }
                catch (IOException)
                {
                    Debug.LogWarning("Load failed (file locked)");
                }
                catch (ArgumentException)
                {
                    Debug.LogWarning("Load failed (file is corrupted or empty)");
                }
                catch (Exception e) // 何があっても再生成する
                {
                    Debug.LogException(e);
                }
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
