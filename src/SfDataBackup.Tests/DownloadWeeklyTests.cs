using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SfDataBackup.Downloaders;
using SfDataBackup.Extractors;

namespace SfDataBackup.Tests
{
    public class DownloadWeeklyTests
    {
        private Mock<ILogger<DownloadWeekly>> loggerMock;
        private Mock<ISfExportLinkExtractor> extractorMock;
        private Mock<ISfExportDownloader> downloaderMock;

        private TimerInfo dummyTimer;

        private DownloadWeekly function;

        [SetUp]
        public void Setup()
        {
            loggerMock = new Mock<ILogger<DownloadWeekly>>();

            // Setup Extractor mock
            var dummyLinks = new List<Uri>
            {
                new Uri("https://my.salesforce.com/download/path")
            };
            var dummyExtractResult = new SfExportLinkExtractorResult(true, dummyLinks);

            extractorMock = new Mock<ISfExportLinkExtractor>();
            extractorMock.Setup(x => x.ExtractAsync())
                         .ReturnsAsync(dummyExtractResult);

            // Setup Downloader mock
            var dummyPaths = new List<string>
            {
                "export/mysfexport.zip"
            };
            var dummyDownloadResult = new SfExportDownloaderResult(true, dummyPaths);

            downloaderMock = new Mock<ISfExportDownloader>();
            downloaderMock.Setup(x => x.DownloadAsync(It.IsAny<IList<Uri>>()))
                          .ReturnsAsync(dummyDownloadResult);

            var schedule = new DailySchedule();
            var status = new ScheduleStatus();
            dummyTimer = new TimerInfo(schedule, status);

            function = new DownloadWeekly(loggerMock.Object, extractorMock.Object, downloaderMock.Object);
        }

        [Test]
        public async Task RunAsync_ExtractsLinks()
        {
            // Act
            await function.RunAsync(dummyTimer);

            // Assert
            extractorMock.Verify(x => x.ExtractAsync());
        }

        [Test]
        public async Task RunAsync_DownloadsLinks()
        {
            // Act
            await function.RunAsync(dummyTimer);

            // Assert
            downloaderMock.Verify(x => x.DownloadAsync(It.IsAny<IList<Uri>>()));
        }
    }
}