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
        public string AccessToken { get; set; }

        private ILogger<SfParallelExportDownloader> logger;
        private IHttpClientFactory httpClientFactory;
        private IFileSystem fileSystem;
        private SfConfig config;

        public SfParallelExportDownloader(ILogger<SfParallelExportDownloader> logger, IHttpClientFactory httpClientFactory, IFileSystem fileSystem, SfConfig config)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.fileSystem = fileSystem;
            this.config = config;
        }

        public async Task<SfExportDownloaderResult> DownloadAsync(string downloadPath, IList<Uri> downloadLinks)
        {
            if (string.IsNullOrWhiteSpace(AccessToken))
                throw new InvalidOperationException("Missing AccessToken");

            var httpClient = httpClientFactory.CreateClient("DefaultClient");

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var downloadTasks = new List<Task<string>>();
            for (var i = 0; i < downloadLinks.Count; i++)
            {
                var link = downloadLinks[i];
                var exportPath = fileSystem.Path.Combine(downloadPath, $"export{i}.zip");

                downloadTasks.Add(DownloadExport(httpClient, link, exportPath, cancellationToken));
            }

            var allDownloadedSuccessfully = true;
            var exportPaths = new List<string>();

            while (downloadTasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(downloadTasks);
                if (completedTask.IsCompletedSuccessfully)
                {
                    var exportPath = await completedTask;
                    exportPaths.Add(exportPath);
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
                return new SfExportDownloaderResult(true, exportPaths);
            else
                return new SfExportDownloaderResult(false);
        }

        public async Task<string> DownloadExport(HttpClient httpClient, Uri downloadLink, string localPath, CancellationToken cancellationToken)
        {
            var request = HttpRequestHelper.CreateRequestWithSalesforceCookie(downloadLink, config.OrganisationId, AccessToken);

            // Send request for export
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
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