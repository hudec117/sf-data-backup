using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SfDataBackup.Consolidators;
using SfDataBackup.Downloaders;
using SfDataBackup.Extractors;

namespace SfDataBackup
{
    public class DownloadWeekly
    {
        private const string schedule = "0 0 16 * * Fri";

        private ILogger<DownloadWeekly> logger;
        private ISfExportLinkExtractor linkExtractor;
        private ISfExportDownloader exportDownloader;
        private ISfExportConsolidator exportConsolidator;
        private IFileSystem fileSystem;

        public DownloadWeekly(
            ILogger<DownloadWeekly> logger,
            ISfExportLinkExtractor linkExtractor,
            ISfExportDownloader exportDownloader,
            ISfExportConsolidator exportConsolidator,
            IFileSystem fileSystem
        )
        {
            this.logger = logger;
            this.linkExtractor = linkExtractor;
            this.exportDownloader = exportDownloader;
            this.exportConsolidator = exportConsolidator;
            this.fileSystem = fileSystem;
        }

        [FunctionName(nameof(DownloadWeekly))]
        public async Task RunAsync(
            [TimerTrigger(schedule, RunOnStartup = true)] TimerInfo timer,
            [Blob("backups/{DateTime}.zip", FileAccess.Write)] Stream exportStream,
            ExecutionContext context
        )
        {
            // 1. EXTRACT LINKS
            logger.LogInformation("Extracting export links from Salesforce.");

            var extractResult = await linkExtractor.ExtractAsync();
            if (!extractResult.Success)
            {
                logger.LogError("Link extractor unsuccessful.");
                return;
            }

            if (extractResult.RelativeUrls.Count == 0)
            {
                logger.LogError("Link extractor returned no links.");
                return;
            }

            // 2. DOWNLOAD
            logger.LogInformation("Downloading exports from Salesforce...");

            var downloadResult = await exportDownloader.DownloadAsync(context.FunctionDirectory, extractResult.RelativeUrls);
            if (!downloadResult.Success)
            {
                logger.LogError("Export downloader unsuccessful.");
                return;
            }

            // 3. CONSOLIDATE
            logger.LogInformation("Consolidating exports...");

            var consolidatedExportPath = Path.Combine(context.FunctionDirectory, "export.zip");
            var consolidatorResult = exportConsolidator.Consolidate(downloadResult.ExportPaths, consolidatedExportPath);
            if (!consolidatorResult.Success)
            {
                logger.LogError("Export consolidator unsuccessful.");
                return;
            }

            // 4. UPLOAD
            logger.LogInformation("Uploading export...");

            using (var fileStream = fileSystem.File.Open(consolidatedExportPath, FileMode.Open))
            {
                await fileStream.CopyToAsync(exportStream);
            }

            // 5. CLEANUP
            logger.LogInformation("Cleaning up...");
        }
    }
}