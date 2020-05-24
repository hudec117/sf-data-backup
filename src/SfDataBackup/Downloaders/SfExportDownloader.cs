using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SfDataBackup.Downloaders
{
    public class SfExportDownloader : ISfExportDownloader
    {
        private ILogger<SfExportDownloader> logger;
        private SfExportDownloaderConfig config;
        private IHttpClientFactory httpClientFactory;
        private IFileSystem fileSystem;

        public SfExportDownloader(ILogger<SfExportDownloader> logger, SfExportDownloaderConfig config, IHttpClientFactory httpClientFactory, IFileSystem fileSystem)
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

                // Send request for export
                HttpResponseMessage response;
                try
                {
                    response = await httpClient.GetAsync(link);
                }
                catch (HttpRequestException exception)
                {
                    logger.LogError(exception, "HTTP request for export failed.");
                    return new SfExportDownloaderResult(false);
                }

                if (!response.IsSuccessStatusCode)
                    return new SfExportDownloaderResult(false);

                // Download the export
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

    public class SfExportDownloaderConfig : SfConfig
    {
        public string DownloadPath { get; set; }

        public SfExportDownloaderConfig(SfConfig config) : base(config)
        {
        }
    }

    public class SfExportDownloaderResult : SfResult
    {
        public IList<string> Paths { get; set; }

        public SfExportDownloaderResult(bool success) : base(success)
        {
        }

        public SfExportDownloaderResult(bool success, IList<string> paths) : base(success)
        {
            Paths = paths;
        }
    }
}