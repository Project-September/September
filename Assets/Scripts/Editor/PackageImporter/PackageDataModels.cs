using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace September.Editor.PackageImporter
{
    [Serializable]
    public struct PackageRelease
    {
        [JsonProperty("id")] public int ID { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("tag_name")] public string TagName { get; set; }
        [JsonProperty("published_at")] public string PublishedAt { get; set; }
        [JsonProperty("assets")] public List<PackageAsset> Assets { get; set; }
    }

    [Serializable]
    public struct PackageAsset
    {
        [JsonProperty("id")] public int ID { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("download_url")] public string URL { get; set; }
        [JsonProperty("size")] public long Size { get; set; }
        public string SourceID { get; set; }
        public string ModifiedTime { get; set; }
        public string Checksum { get; set; }
        public PackageSourceType SourceType { get; set; }
    }

    public enum PackageSourceType
    {
        Api,
        GoogleDrive
    }
}
