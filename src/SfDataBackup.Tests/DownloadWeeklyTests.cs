using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SfDataBackup.Extractors;

namespace SfDataBackup.Tests
{
    public class DownloadWeeklyTests
    {
        private Mock<ILogger<DownloadWeekly>> loggerMock;
        private Mock<ISfExportLinkExtractor> extractorMock;

        private TimerInfo dummyTimer;

        private DownloadWeekly function;

        [SetUp]
        public void Setup()
        {
            loggerMock = new Mock<ILogger<DownloadWeekly>>();

            extractorMock = new Mock<ISfExportLinkExtractor>();
            var dummyResult = new SfExportLinkExtractorResult(true, null);
            extractorMock.Setup(x => x.ExtractAsync())
                         .ReturnsAsync(dummyResult);

            var schedule = new DailySchedule();
            var status = new ScheduleStatus();
            dummyTimer = new TimerInfo(schedule, status);

            function = new DownloadWeekly(loggerMock.Object, extractorMock.Object);
        }

        [Test]
        public async Task RunAsync_ExtractsLinks()
        {
            // Act
            await function.RunAsync(dummyTimer);

            // Assert
            extractorMock.Verify(x => x.ExtractAsync());
        }
    }
}