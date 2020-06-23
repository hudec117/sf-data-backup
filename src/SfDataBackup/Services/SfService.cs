using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SfDataBackup.Services
{
    public class SfService : ISfService
    {
        private ILogger<SfService> logger;
        private IHttpClientFactory httpClientFactory;
        private SfOptions options;

        public SfService(
            ILogger<SfService> logger,
            IHttpClientFactory httpClientFactory,
            IOptionsSnapshot<SfOptions> optionsProvider
        )
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.options = optionsProvider.Value;
        }

        public Task<string> GetPageSourceAsync(string relativeUrl)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> DownloadFileAsync(string relativeUrl)
        {
            throw new NotImplementedException();
        }
    }
}