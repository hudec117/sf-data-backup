using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using SfDataBackup.Extractors;

namespace SfDataBackup
{
    public class DownloadWeekly
    {
        private ILogger<DownloadWeekly> logger;
        private ISfExportLinkExtractor linkExtractor;

        public DownloadWeekly(ILogger<DownloadWeekly> logger, ISfExportLinkExtractor linkExtractor)
        {
            this.logger = logger;
            this.linkExtractor = linkExtractor;
        }

        [FunctionName(nameof(DownloadWeekly))]
        public void Run([TimerTrigger("0 0 16 * * Fri", RunOnStartup = true)]TimerInfo timer)
        {
            logger.LogInformation("Test");
        }
    }
}
