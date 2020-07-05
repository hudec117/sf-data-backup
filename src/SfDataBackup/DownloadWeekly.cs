using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SfDataBackup.Consolidators;
using SfDataBackup.Services;

namespace SfDataBackup
{
    public class DownloadWeekly
    {
        private ILogger<DownloadWeekly> logger;
        private ISfService service;
        private IZipFileConsolidator consolidator;
        private IFileSystem fileSystem;

        public DownloadWeekly(
            ILogger<DownloadWeekly> logger,
            ISfService service,
            IZipFileConsolidator consolidator,
            IFileSystem fileSystem
        )
        {
            this.logger = logger;
            this.service = service;
            this.consolidator = consolidator;
            this.fileSystem = fileSystem;
        }

        [FunctionName(nameof(DownloadWeekly))]
        [StorageAccount("BackupStorage")]
        public async Task RunAsync(
            [TimerTrigger("%Schedule%", RunOnStartup = true)] TimerInfo timer,
            [Blob("backups/{DateTime}.zip", FileAccess.Write)] ICloudBlob blob,
            ExecutionContext context
        )
        {
            // 1. EXTRACT LINKS
            logger.LogInformation("Extracting export links from Salesforce...");

            var exportDownloadLinks = await service.GetExportDownloadLinksAsync();
            if (exportDownloadLinks.Count == 0)
                throw new DownloadWeeklyException("No export download links found.");

            // 2. DOWNLOAD
            logger.LogInformation("Downloading exports from Salesforce...");

            var downloadExportPaths = await service.DownloadExportsAsync(exportDownloadLinks);

            // 3. CONSOLIDATE
            logger.LogInformation("Consolidating exports...");

            string consolidatedExportPath;
            try
            {
                consolidatedExportPath = consolidator.Consolidate(downloadExportPaths);
            }
            catch (ConsolidationException exception)
            {
                throw new DownloadWeeklyException("Consolidator unsuccessful.", exception);
            }

            // 4. UPLOAD
            logger.LogInformation("Uploading export...");

            try
            {
                await blob.UploadFromFileAsync(consolidatedExportPath);
            }
            catch (StorageException exception)
            {
                throw new DownloadWeeklyException("Failed to upload consolidated export.", exception);
            }

            // 5. CLEANUP
            logger.LogInformation("Cleaning up");

            fileSystem.File.Delete(consolidatedExportPath);

            logger.LogInformation("Done");
        }
    }

    [System.Serializable]
    public class DownloadWeeklyException : System.Exception
    {
        public DownloadWeeklyException() { }

        public DownloadWeeklyException(string message) : base(message) { }

        public DownloadWeeklyException(string message, System.Exception inner) : base(message, inner) { }

        protected DownloadWeeklyException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context
        ) : base(info, context) { }
    }
}