using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SfDataBackup.Extractors
{
    public class SfExportLinkExtractor : ISfExportLinkExtractor
    {
        private ILogger<SfExportLinkExtractor> logger;
        private SfExportLinkExtractorConfig config;
        private HttpClient httpClient;

        public SfExportLinkExtractor(ILogger<SfExportLinkExtractor> logger, SfExportLinkExtractorConfig config, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.config = config;
            this.httpClient = httpClientFactory.CreateClient();
        }

        public Task<SfExportLinkExtractorResult> ExtractAsync()
        {
            var requestUrl = new Uri(config.OrganisationUrl, config.ExportServicePath);

            // Construct headers with cookie for Salesforce
            var cookieContainer = new CookieContainer();
            var oidCookie = new Cookie("oid", config.OrganisationId);
            var sidCookie = new Cookie("sid", config.AccessToken);
            cookieContainer.Add(requestUrl, oidCookie);
            cookieContainer.Add(requestUrl, sidCookie);

            var cookieHeader = cookieContainer.GetCookieHeader(requestUrl);

            // Get the export data page
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Cookie", cookieHeader);

            return Task.FromResult(new SfExportLinkExtractorResult());
        }
    }

    public class SfExportLinkExtractorConfig
    {
        public Uri OrganisationUrl { get; set; }

        public string OrganisationId { get; set; }

        public string AccessToken { get; set; }

        public string ExportServicePath { get; set; }

        public string ExportServiceRegex { get; set; }
    }

    public class SfExportLinkExtractorResult
    {
        public bool Success { get; set; }

        public IList<string> Links { get; set; }
    }
}