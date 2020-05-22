using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace SfDataBackup.Extractors
{
    public class DummyExtractor : ISfExportLinkExtractor
    {
        public Task<SfExportLinkExtractorResult> ExtractAsync()
        {
            return Task.FromResult(new SfExportLinkExtractorResult());
        }
    }
}