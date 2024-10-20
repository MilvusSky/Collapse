﻿using CollapseLauncher.Helper;
using Hi3Helper;
using Hi3Helper.Data;
using Hi3Helper.Http;
using Hi3Helper.Shared.ClassStruct;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CollapseLauncher
{
    internal partial class ZenlessRepair
    {
        private async Task<bool> Repair(List<FilePropertiesRemote> repairAssetIndex, CancellationToken token)
        {
            // Set total activity string as "Waiting for repair process to start..."
            _status.ActivityStatus = Locale.Lang._GameRepairPage.Status11;
            _status.IsProgressAllIndetermined = true;
            _status.IsProgressPerFileIndetermined = true;

            // Update status
            UpdateStatus();

            // Reset stopwatch
            RestartStopwatch();

            // Initialize new proxy-aware HttpClient
            using HttpClient client = new HttpClientBuilder<SocketsHttpHandler>()
                .UseLauncherConfig(_downloadThreadCount + _downloadThreadCountReserved)
                .SetUserAgent(_userAgent)
                .SetAllowedDecompression(DecompressionMethods.None)
                .Create();

            // Use the new DownloadClient instance
            DownloadClient downloadClient = DownloadClient.CreateInstance(client);

            // Iterate repair asset and check it using different method for each type
            ObservableCollection<IAssetProperty> assetProperty = new ObservableCollection<IAssetProperty>(AssetEntry);
            if (_isBurstDownloadEnabled)
            {
                await Parallel.ForEachAsync(
                    PairEnumeratePropertyAndAssetIndexPackage(
#if ENABLEHTTPREPAIR
                    EnforceHTTPSchemeToAssetIndex(repairAssetIndex)
#else
                    repairAssetIndex
#endif
                    , assetProperty),
                    new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = _downloadThreadCount },
                    async (asset, innerToken) =>
                    {
                        // Assign a task depends on the asset type
                        Task assetTask = asset.AssetIndex.FT switch
                        {
                            FileType.Block => RepairAssetTypeGeneric(asset, downloadClient, _httpClient_RepairAssetProgress, innerToken),
                            FileType.Audio => RepairAssetTypeGeneric(asset, downloadClient, _httpClient_RepairAssetProgress, innerToken),
                            FileType.Video => RepairAssetTypeGeneric(asset, downloadClient, _httpClient_RepairAssetProgress, innerToken),
                            _ => RepairAssetTypeGeneric(asset, downloadClient, _httpClient_RepairAssetProgress, innerToken)
                        };

                        // Await the task
                        await assetTask;
                    });
            }
            else
            {
                foreach ((FilePropertiesRemote AssetIndex, IAssetProperty AssetProperty) asset in
                    PairEnumeratePropertyAndAssetIndexPackage(
#if ENABLEHTTPREPAIR
                    EnforceHTTPSchemeToAssetIndex(repairAssetIndex)
#else
                    repairAssetIndex
#endif
                    , assetProperty))
                {
                    // Assign a task depends on the asset type
                    Task assetTask = asset.AssetIndex.FT switch
                    {
                        FileType.Block => RepairAssetTypeGeneric(asset, downloadClient, _httpClient_RepairAssetProgress, token),
                        FileType.Audio => RepairAssetTypeGeneric(asset, downloadClient, _httpClient_RepairAssetProgress, token),
                        FileType.Video => RepairAssetTypeGeneric(asset, downloadClient, _httpClient_RepairAssetProgress, token),
                        _ => RepairAssetTypeGeneric(asset, downloadClient, _httpClient_RepairAssetProgress, token)
                    };

                    // Await the task
                    await assetTask;
                }
            }

            return true;
        }

        #region GenericRepair
        private async Task RepairAssetTypeGeneric((FilePropertiesRemote AssetIndex, IAssetProperty AssetProperty) asset, DownloadClient downloadClient, DownloadProgressDelegate downloadProgress, CancellationToken token)
        {
            // Increment total count current
            _progressAllCountCurrent++;
            // Set repair activity status
            UpdateRepairStatus(
                string.Format(Locale.Lang._GameRepairPage.Status8, Path.GetFileName(asset.AssetIndex.N)),
                string.Format(Locale.Lang._GameRepairPage.PerProgressSubtitle2, ConverterTool.SummarizeSizeSimple(_progressAllSizeCurrent), ConverterTool.SummarizeSizeSimple(_progressAllSizeTotal)),
                true);

            // If asset type is unused, then delete it
            if (asset.AssetIndex.FT == FileType.Unused)
            {
                FileInfo fileInfo = new FileInfo(asset.AssetIndex.N!);
                if (fileInfo.Exists)
                {
                    fileInfo.IsReadOnly = false;
                    fileInfo.Delete();
                    Logger.LogWriteLine($"File [T: {asset.AssetIndex.FT}] {(asset.AssetIndex.FT == FileType.Block ? asset.AssetIndex.CRC : asset.AssetIndex.N)} deleted!", LogType.Default, true);
                }
            }
            else
            {
                // Start asset download task
                await RunDownloadTask(asset.AssetIndex.S, asset.AssetIndex.N!, asset.AssetIndex.RN, downloadClient, downloadProgress, token);
                Logger.LogWriteLine($"File [T: {asset.AssetIndex.FT}] {(asset.AssetIndex.FT == FileType.Block ? asset.AssetIndex.CRC : asset.AssetIndex.N)} has been downloaded!", LogType.Default, true);
            }

            // Pop repair asset display entry
            PopRepairAssetEntry(asset.AssetProperty);
        }
        #endregion
    }
}