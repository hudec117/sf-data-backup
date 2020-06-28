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
        private ILogger<DownloadWeekly> logger;
        private ISfService service;
        private IZipFileConsolidator exportConsolidator;
        private IFileSystem fileSystem;

        public DownloadWeekly(
            ILogger<DownloadWeekly> logger,
            ISfService service,
            IZipFileConsolidator exportConsolidator,
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
            [TimerTrigger("%Schedule%", RunOnStartup = true)] TimerInfo timer,
            [Blob("backups/{DateTime}.zip", FileAccess.Write)] Stream exportStream,
            ExecutionContext context
        )
        {
            // 1. EXTRACT LINKS
            logger.LogInformation("Extracting export links from Salesforce...");

            var exportDownloadLinks = await service.GetExportDownloadLinksAsync();
            if (exportDownloadLinks.Count == 0)
            {
                logger.LogWarning(
                    "No export download links found. Check:\n" +
                    "a) exports are available on Weekly Export Service page\n" +
                    "b) \"Salesforce:ExportService:Page\" is a relative URL to the Weekly Export Service page\n" +
                    "c) \"Salesforce:ExportService:Regex\" still applies to the Weekly Export Service page source"
                );
                return;
            }

            // 2. DOWNLOAD
            logger.LogInformation("Downloading exports from Salesforce...");

            var downloadExportPaths = await service.DownloadExportsAsync(context.FunctionDirectory, exportDownloadLinks);

            // 3. CONSOLIDATE
            logger.LogInformation("Consolidating exports...");

            var consolidatedExportPath = fileSystem.Path.Combine(context.FunctionDirectory, "export.zip");

            try
            {
                exportConsolidator.Consolidate(downloadExportPaths, consolidatedExportPath);
            }
            catch (ConsolidationException exception)
            {
                logger.LogError(exception, "Consolidator unsuccessful.");
                return;
            }

            // 4. UPLOAD
            logger.LogInformation("Uploading export...");

            using (var fileStream = fileSystem.File.Open(consolidatedExportPath, FileMode.Open))
            {
                await fileStream.CopyToAsync(exportStream);
            }

            // 5. CLEANUP
            logger.LogInformation("Cleaning up");

            logger.LogInformation("Done");
        }
    }
}