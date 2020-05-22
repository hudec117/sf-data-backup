using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace SfDataBackup.Extractors
{
    public class SfExportLinkExtractor : ISfExportLinkExtractor
    {
        private SfExportLinkExtractorConfig config;
        private HttpClient httpClient;

        public SfExportLinkExtractor(SfExportLinkExtractorConfig config, HttpClient httpClient)
        {
            this.config = config;
            this.httpClient = httpClient;
        }

        public Task<SfExportLinkExtractorResult> ExtractAsync()
        {
            return Task.FromResult(new SfExportLinkExtractorResult());
        }
    }

    public class SfExportLinkExtractorConfig
    {
        public string ServerUrl { get; set; }

        public string AccessToken { get; set; }

        public string OrganisationId { get; set; }
    }

    public class SfExportLinkExtractorResult
    {
        public bool Success { get; set; }

        public IList<string> Links { get; set; }
    }
}