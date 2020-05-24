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

            var localPaths = new List<string>();

            for (var i = 0; i < exportDownloadLinks.Count; i++)
            {
                var link = exportDownloadLinks[i];

                HttpResponseMessage response;
                try
                {
                    response = await httpClient.GetAsync(link);
                }
                catch (HttpRequestException exception)
                {
                    logger.LogError(exception, "HTTP request for {link} failed.", link);
                    return new SfExportDownloaderResult(false);
                }

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("HTTP {code} received for {link}", link);
                    return new SfExportDownloaderResult(false);
                }

                var localPath = fileSystem.Path.Combine(config.DownloadPath, $"export{i}.zip");
                using (var fileStream = fileSystem.File.Create(localPath))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                localPaths.Add(localPath);
            }

            return new SfExportDownloaderResult(true, localPaths);
        }
    }
}