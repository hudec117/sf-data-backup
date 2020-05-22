using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace SfDataBackup.Extractors
{
    public class SfExportLinkExtractor : ISfExportLinkExtractor
    {
        public SfExportLinkExtractorConfig Config { get; set; }

        public SfExportLinkExtractor(SfExportLinkExtractorConfig config)
        {
            Config = config;
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