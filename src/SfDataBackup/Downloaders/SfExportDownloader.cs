using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SfDataBackup.Services;

namespace SfDataBackup.Downloaders
{
    public class SfExportDownloader : ISfExportDownloader
    {
        private ILogger<SfExportDownloader> logger;
        private ISfService service;
        private IFileSystem fileSystem;

        public SfExportDownloader(
            ILogger<SfExportDownloader> logger,
            ISfService service,
            IFileSystem fileSystem
        )
        {
            this.logger = logger;
            this.service = service;
            this.fileSystem = fileSystem;
        }

        public async Task<SfExportDownloaderResult> DownloadAsync(string downloadPath, IList<string> downloadLinks)
        {
            var exportPaths = new List<string>();

            for (var i = 0; i < downloadLinks.Count; i++)
            {
                var link = downloadLinks[i];

                var exportPath = fileSystem.Path.Combine(downloadPath, $"export{i}.zip");

                logger.LogInformation("Downloading export from {link}...", link);

                using(var downloadFileStream = await service.DownloadFileAsync(link))
                {
                    using (var fileStream = fileSystem.File.Create(exportPath))
                    {
                        await downloadFileStream.CopyToAsync(fileStream);
                    }
                }

                logger.LogInformation("Downloaded export to {path}", exportPath);

                exportPaths.Add(exportPath);
            }

            return new SfExportDownloaderResult(true, exportPaths);
        }
    }

    public class SfExportDownloaderResult : SfResult
    {
        public IList<string> ExportPaths { get; set; }

        public SfExportDownloaderResult(bool success) : base(success)
        {
        }

        public SfExportDownloaderResult(bool success, IList<string> exportPaths) : base(success)
        {
            ExportPaths = exportPaths;
        }
    }
}