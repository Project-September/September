using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace September.Editor.AssetImporter
{
    public struct ProgressInfo
    {
        public string Status { get; set; }
        public float Progress { get; set; }
        public string Detail { get; set; }
    }

    public interface IProgressReporter
    {
        event Action<ProgressInfo> OnProgressChanged;
        void ReportProgress(ProgressInfo progress);
    }

    public interface IAssetDownloader
    {
        Task<string> DownloadAssetAsync(string route, int assetId, CancellationToken cancellationToken);
    }

    public interface IFileExtractor
    {
        Task<string> ExtractZipFileAsync(string zipPath, string extractPath, CancellationToken cancellationToken);
    }

    public interface IReleaseService
    {
        Task<List<Release>> GetReleasesAsync(string route, CancellationToken cancellationToken);
    }

    public interface IAssetImportService
    {
        Task<List<Release>> GetReleasesAsync(string route, CancellationToken cancellationToken);
        Task<string> DownloadAndExtractAssetAsync(string route, List<Asset> assets, CancellationToken cancellationToken);
        void ImportUnityPackages(string extractPath, bool showImportDialog = false);
        event Action<ProgressInfo> OnProgressChanged;
    }

    public interface IAssetImportController
    {
        bool IsImporting { get; }
        List<string> ReleaseNames { get; }
        int SelectedReleaseIndex { get; set; }

        Task InitializeAsync();
        Task ImportSelectedAssetAsync();

        event Action<ProgressInfo> OnProgressChanged;
        event Action<string> OnStatusChanged;
    }
}