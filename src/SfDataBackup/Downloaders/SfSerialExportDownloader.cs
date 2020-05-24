using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SfDataBackup.Downloaders
{
    public class SfSerialExportDownloader : ISfExportDownloader
    {
        private ILogger<SfSerialExportDownloader> logger;
        private SfExportDownloaderConfig config;
        private IHttpClientFactory httpClientFactory;
        private IFileSystem fileSystem;

        public SfSerialExportDownloader(ILogger<SfSerialExportDownloader> logger, SfExportDownloaderConfig config, IHttpClientFactory httpClientFactory, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.config = config;
            this.httpClientFactory = httpClientFactory;
            this.fileSystem = fileSystem;
        }

        public async Task<SfExportDownloaderResult> DownloadAsync(IList<Uri> exportDownloadLinks)
        {
            var httpClient = httpClientFactory.CreateClient("SalesforceClient");

            fileSystem.Directory.CreateDirectory(config.DownloadPath);

            for (var i = 0; i < exportDownloadLinks.Count; i++)
            {
                var link = exportDownloadLinks[i];

                var response = await httpClient.GetAsync(link);

                var downloadPath = fileSystem.Path.Combine(config.DownloadPath, $"export{i}.zip");
                using (var fileStream = fileSystem.File.Create(downloadPath))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }

            return new SfExportDownloaderResult(true);
        }

        public async Task<string> DownloadExport(HttpClient httpClient, Uri downloadLink, string localPath, CancellationToken cancellationToken)
        {
            // Send request for export
            var response = await httpClient.GetAsync(downloadLink, HttpCompletionOption.ResponseContentRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(response.ReasonPhrase);

            // Download the export
            using (var fileStream = fileSystem.File.Create(localPath))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            return localPath;
        }
    }
}