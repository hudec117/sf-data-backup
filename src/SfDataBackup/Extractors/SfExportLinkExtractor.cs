using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SfDataBackup.Extractors
{
    public class SfExportLinkExtractor : ISfExportLinkExtractor
    {
        private ILogger<SfExportLinkExtractor> logger;
        private SfExportLinkExtractorConfig config;
        private IHttpClientFactory httpClientFactory;
        private HttpClient httpClient;

        public SfExportLinkExtractor(ILogger<SfExportLinkExtractor> logger, SfExportLinkExtractorConfig config, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.config = config;
            this.httpClientFactory = httpClientFactory;
            this.httpClient = httpClientFactory.CreateClient();
        }

        public async Task<SfExportLinkExtractorResult> ExtractAsync()
        {
            var requestUrl = new Uri(config.OrganisationUrl, config.ExportServicePath);

            // Create HTTP client and send request
            var client = this.httpClientFactory.CreateClient("SalesforceClient");

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(requestUrl);
            }
            catch (HttpRequestException exception)
            {
                logger.LogError(exception, "HTTP request to Salesforce Data Export failed.");

                return new SfExportLinkExtractorResult(false);
            }

            var source = await response.Content.ReadAsStringAsync();

            // Extract links from source
            var links = GetLinksFromSource(source);

            return new SfExportLinkExtractorResult(true, links);
        }

        public List<Uri> GetLinksFromSource(string source)
        {
            var links = new List<Uri>();

            var matches = Regex.Matches(source, config.ExportServiceRegex, RegexOptions.IgnoreCase);

            // Explicit type is required
            foreach (Match match in matches)
            {
                var relativeUrlGroup = match.Groups["relurl"];
                var relativeUrl = relativeUrlGroup.Value;

                // Remove &amp; from relative URL
                relativeUrl = relativeUrl.Replace("&amp;", "&");

                var fullUrl = new Uri(config.OrganisationUrl, relativeUrl);
                links.Add(fullUrl);
            }

            return links;
        }
    }

    public class SfExportLinkExtractorConfig : SfConfig
    {
        public string ExportServicePath { get; set; }

        public string ExportServiceRegex { get; set; }

        public SfExportLinkExtractorConfig(SfConfig config) : base(config)
        {
        }
    }

    public class SfExportLinkExtractorResult : SfResult
    {
        public IList<Uri> Links { get; set; }

        public SfExportLinkExtractorResult(bool success) : base(success)
        {
        }

        public SfExportLinkExtractorResult(bool success, IList<Uri> links) : base(success)
        {
            Links = links;
        }
    }
}