using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SfDataBackup.Downloaders
{
    public class SfParallelExportDownloader : ISfExportDownloader
    {
        private ILogger<SfSerialExportDownloader> logger;
        private SfExportDownloaderConfig config;
        private IHttpClientFactory httpClientFactory;
        private IFileSystem fileSystem;

        public SfParallelExportDownloader(ILogger<SfSerialExportDownloader> logger, SfExportDownloaderConfig config, IHttpClientFactory httpClientFactory, IFileSystem fileSystem)
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

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var downloadTasks = new List<Task<string>>();
            for (var i = 0; i < exportDownloadLinks.Count; i++)
            {
                var link = exportDownloadLinks[i];
                var localPath = fileSystem.Path.Combine(config.DownloadPath, $"export{i}.zip");

                downloadTasks.Add(DownloadExport(httpClient, link, localPath, cancellationToken));
            }

            var allDownloadedSuccessfully = true;
            var localPaths = new List<string>();

            while (downloadTasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(downloadTasks);
                if (completedTask.IsCompletedSuccessfully)
                {
                    var localPath = await completedTask;
                    localPaths.Add(localPath);
                }
                else if (completedTask.IsFaulted)
                {
                    cancellationTokenSource.Cancel();

                    logger.LogError(completedTask.Exception, "HTTP request for export failed.");
                    allDownloadedSuccessfully = false;
                }

                downloadTasks.Remove(completedTask);
            }

            if (allDownloadedSuccessfully)
                return new SfExportDownloaderResult(true, localPaths);
            else
                return new SfExportDownloaderResult(false);
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