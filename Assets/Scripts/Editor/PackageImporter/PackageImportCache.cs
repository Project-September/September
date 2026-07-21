using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace September.Editor.PackageImporter
{
    [Serializable]
    public class ImportRecord
    {
        public string fileId;
        public string fileName;
        public string importedModifiedTime;
        public string importedAtLocal;
    }

    [Serializable]
    internal class ImportRecordListWrapper
    {
        public List<ImportRecord> records = new List<ImportRecord>();
    }

    /// <summary>
    /// どのパッケージをどのバージョンでインポート済みかをローカルに保存するキャッシュ
    /// Gitなどバージョン管理には含まない
    /// </summary>
    internal static class PackageImportCache
    {
        private static string CachePath
        {
            get
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                return Path.Combine(projectRoot, "Library", "PackageImporterCache.Json");
            }
        }

        private static ImportRecordListWrapper _cache;

        private static ImportRecordListWrapper Cache
        {
            get
            {
                if (_cache == null) Load();
                return _cache;
            }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(CachePath))
                {
                    string json = File.ReadAllText(CachePath);
                    _cache = JsonUtility.FromJson<ImportRecordListWrapper>(json) ?? new ImportRecordListWrapper();
                }
                else
                {
                    _cache = new ImportRecordListWrapper();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PackageImporter] キャッシュ読み込みに失敗しました: {e.Message}");
                _cache = new ImportRecordListWrapper();
            }
        }

        public static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(CachePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(CachePath, JsonUtility.ToJson(Cache, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PackageImporter] キャッシュ保存に失敗しました: {e.Message}");
            }
        }

        public static ImportRecord Find(string filedId)
        {
            return Cache.records.Find(r => r.fileId == filedId);
        }

        public static void Update(string fileId, string fileName, string importedModifiedTime)
        {
            var record = Find(filedId);
            if (record == null)
            {
                record = new ImportRecord { fileId = fileId };
                Cache.records.Add(record);
            }

            record.fileName = fileName;
            record.importedModifiedTime = importedModifiedTime;
            record.importedAtLocal = DateTime.Now.ToString("O");
            Save();
        }
    }
}