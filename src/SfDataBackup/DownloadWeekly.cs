using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SfDataBackup.Consolidators;
using SfDataBackup.Services;

namespace SfDataBackup
{
    public class DownloadWeekly
    {
        private const string schedule = "0 0 16 * * Fri";

        private ILogger<DownloadWeekly> logger;
        private ISfService service;
        private ISfExportConsolidator exportConsolidator;
        private IFileSystem fileSystem;

        public DownloadWeekly(
            ILogger<DownloadWeekly> logger,
            ISfService service,
            ISfExportConsolidator exportConsolidator,
            IFileSystem fileSystem
        )
        {
            this.logger = logger;
            this.service = service;
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

            var exportDownloadLinks = await service.GetExportDownloadLinksAsync();
            if (exportDownloadLinks.Count == 0)
            {
                logger.LogWarning("No export download links found.");
                return;
            }

            // 2. DOWNLOAD
            logger.LogInformation("Downloading exports from Salesforce...");

            var downloadExportPaths = await service.DownloadExportsAsync(context.FunctionDirectory, exportDownloadLinks);

            // 3. CONSOLIDATE
            logger.LogInformation("Consolidating exports...");

            var consolidatedExportPath = Path.Combine(context.FunctionDirectory, "export.zip");
            var consolidatorResult = exportConsolidator.Consolidate(downloadExportPaths, consolidatedExportPath);
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