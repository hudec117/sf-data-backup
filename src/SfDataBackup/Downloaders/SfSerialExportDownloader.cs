using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SfDataBackup.Downloaders
{
    public class SfSerialExportDownloader : ISfExportDownloader
    {
        private ILogger<SfSerialExportDownloader> logger;
        private IHttpClientFactory httpClientFactory;
        private IFileSystem fileSystem;

        public SfSerialExportDownloader(ILogger<SfSerialExportDownloader> logger, IHttpClientFactory httpClientFactory, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.fileSystem = fileSystem;
        }

        public async Task<SfExportDownloaderResult> DownloadAsync(string downloadPath, IList<Uri> downloadLinks)
        {
            var httpClient = httpClientFactory.CreateClient("SalesforceClient");

            var exportPaths = new List<string>();

            for (var i = 0; i < downloadLinks.Count; i++)
            {
                var link = downloadLinks[i];

                logger.LogInformation("Downloading export {link}", link);

                HttpResponseMessage response;
                try
                {
                    response = await httpClient.GetAsync(link);
                }
                catch (HttpRequestException exception)
                {
                    logger.LogError(exception, "HTTP request for export {link} failed.", link);
                    return new SfExportDownloaderResult(false);
                }

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("HTTP {code} received for export {link}", link);
                    return new SfExportDownloaderResult(false);
                }

                var exportPath = fileSystem.Path.Combine(downloadPath, $"export{i}.zip");
                using (var fileStream = fileSystem.File.Create(exportPath))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                logger.LogInformation("Downloaded export to {path}", exportPath);

                exportPaths.Add(exportPath);
            }

            return new SfExportDownloaderResult(true, exportPaths);
        }
    }
}