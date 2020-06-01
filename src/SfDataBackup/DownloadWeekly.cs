using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using SfDataBackup.Downloaders;
using SfDataBackup.Extractors;

namespace SfDataBackup
{
    public class DownloadWeekly
    {
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
        public async Task RunAsync([TimerTrigger("0 0 16 * * Fri", RunOnStartup = true)]TimerInfo timer)
        {
            logger.LogInformation("Extracting ZIP links from Salesforce.");

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

            logger.LogInformation("Starting export downloads...");

            var downloadResult = await exportDownloader.DownloadAsync(extractResult.Links);
            if (!downloadResult.Success)
            {
                logger.LogWarning("Export downloader unsuccessful.");
                return;
            }

            logger.LogInformation("Consolidating ZIP files.");
        }
    }
}