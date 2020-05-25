using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SfDataBackup.Extractors;
using SfDataBackup.Downloaders;

namespace SfDataBackup
{
    public class DownloadExport
    {
        private ILogger<DownloadExport> logger;
        private ISfExportLinkExtractor linkExtractor;
        private ISfExportDownloader exportDownloader;

        public DownloadExport(ILogger<DownloadExport> logger, ISfExportLinkExtractor linkExtractor, ISfExportDownloader exportDownloader)
        {
            this.logger = logger;
            this.linkExtractor = linkExtractor;
            this.exportDownloader = exportDownloader;
        }

        [FunctionName("DownloadExport")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get")]HttpRequest request,
            ExecutionContext context
        )
        {
            logger.LogInformation("Extracting export links from Salesforce...");

            var extractResult = await linkExtractor.ExtractAsync();
            if (!extractResult.Success)
            {
                logger.LogWarning("Link extractor unsuccessful.");
                return new StatusCodeResult(500);
            }

            if (extractResult.Links.Count == 0)
            {
                logger.LogWarning("Link extractor returned no links.");
                return new NotFoundResult();
            }

            logger.LogInformation("Downloading exports from Salesforce...");

            var downloadResult = await exportDownloader.DownloadAsync(context.FunctionDirectory, extractResult.Links);
            if (!downloadResult.Success)
            {
                logger.LogWarning("Export downloader unsuccessful.");
                return new StatusCodeResult(500);
            }

            logger.LogInformation("Consolidating exports...");

            logger.LogInformation("Uploading exports...");

            logger.LogInformation("Cleaning up...");

            return new OkResult();
        }
    }
}