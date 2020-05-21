using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace SfDataBackup
{
    public static class DownloadWeekly
    {
        [FunctionName(nameof(DownloadWeekly))]
        public static void Run([TimerTrigger("0 0 16 * * Fri", RunOnStartup = true)]TimerInfo timer, ILogger logger)
        {
            
        }
    }
}
