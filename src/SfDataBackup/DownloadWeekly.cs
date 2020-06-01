using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
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

        public DownloadWeekly(ILogger<DownloadWeekly> logger, ISfExportLinkExtractor linkExtractor, ISfExportDownloader exportDownloader)
        {
            this.logger = logger;
            this.linkExtractor = linkExtractor;
            this.exportDownloader = exportDownloader;
        }

        [FunctionName(nameof(DownloadWeekly))]
        public async Task RunAsync(
            [TimerTrigger(schedule, RunOnStartup = true)] TimerInfo timer,
            [Blob("backups/{DateTime}.zip", FileAccess.Write)] Stream exportStream,
            ExecutionContext context
        )
        {
            logger.LogInformation("Extracting export links from Salesforce.");

            var extractResult = await linkExtractor.ExtractAsync();
            if (!extractResult.Success)
            {
                logger.LogWarning("Link extractor unsuccessful.");
                return;
            }

            if (extractResult.Links.Count == 0)
            {
                logger.LogWarning("Link extractor returned no links.");
                return;
            }

            logger.LogInformation("Downloading exports from Salesforce...");

            var downloadResult = await exportDownloader.DownloadAsync(context.FunctionDirectory, extractResult.Links);
            if (!downloadResult.Success)
            {
                logger.LogWarning("Export downloader unsuccessful.");
                return;
            }

            logger.LogInformation("Consolidating exports...");

            logger.LogInformation("Uploading exports...");

            logger.LogInformation("Cleaning up...");
        }
    }
}