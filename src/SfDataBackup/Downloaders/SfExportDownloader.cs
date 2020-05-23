using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SfDataBackup.Downloaders
{
    public class SfExportDownloader : ISfExportDownloader
    {
        private IHttpClientFactory httpClientFactory;

        public SfExportDownloader(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public Task<SfExportDownloaderResult> DownloadAsync(IList<Uri> exportDownloadLinks)
        {
            return Task.FromResult(new SfExportDownloaderResult(true));
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