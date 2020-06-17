using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SfDataBackup.Consolidators;
using SfDataBackup.Downloaders;
using SfDataBackup.Extractors;
using SfDataBackup.Tests.Data;

namespace SfDataBackup.Tests
{
    public class DownloadWeeklyTests
    {
        private Mock<ILogger<DownloadWeekly>> loggerMock;
        private Mock<ISfExportLinkExtractor> extractorMock;
        private Mock<ISfExportDownloader> downloaderMock;
        private Mock<ISfExportConsolidator> consolidatorMock;
        private MockFileSystem fileSystemMock;

        private TimerInfo dummyTimer;
        private MemoryStream dummyStream;
        private ExecutionContext dummyExecutionContext;

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
                "exports/mysfexport.zip"
            };
            var dummyDownloadResult = new SfExportDownloaderResult(true, dummyPaths);

            downloaderMock = new Mock<ISfExportDownloader>();
            downloaderMock.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<IList<Uri>>()))
                          .ReturnsAsync(dummyDownloadResult);

            consolidatorMock = new Mock<ISfExportConsolidator>();
            consolidatorMock.Setup(x => x.Consolidate(It.IsAny<IList<string>>(), It.IsAny<string>()))
                            .Callback(() => {
                                var fileToAdd = Path.Combine(dummyExecutionContext.FunctionDirectory, "export.zip");
                                fileSystemMock.AddFile(fileToAdd, new MockFileData(TestData.Export));
                            })
                            .Returns(new SfResult(true));

            fileSystemMock = new MockFileSystem();

            var schedule = new DailySchedule();
            var status = new ScheduleStatus();
            dummyTimer = new TimerInfo(schedule, status);

            dummyStream = new MemoryStream();

            dummyExecutionContext = new ExecutionContext
            {
                FunctionDirectory = "C:\\myfuncapp\\DownloadWeekly"
            };

            function = new DownloadWeekly(loggerMock.Object, extractorMock.Object, downloaderMock.Object, consolidatorMock.Object, fileSystemMock);
        }

        [TearDown]
        public void Teardown()
        {
            dummyStream.Dispose();
        }

        [Test]
        public async Task RunAsync_ExtractsLinks()
        {
            // Act
            await function.RunAsync(dummyTimer, dummyStream, dummyExecutionContext);

            // Assert
            extractorMock.Verify(x => x.ExtractAsync());
        }

        [Test]
        public async Task RunAsync_DownloadsLinks()
        {
            // Act
            await function.RunAsync(dummyTimer, dummyStream, dummyExecutionContext);

            // Assert
            downloaderMock.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<IList<Uri>>()));
        }

        [Test]
        public async Task RunAsync_ExtractionFails_DoesNotDownloadLinks()
        {
            // Arrange
            extractorMock.Setup(x => x.ExtractAsync())
                         .ReturnsAsync(new SfExportLinkExtractorResult(false));

            // Act
            await function.RunAsync(dummyTimer, dummyStream, dummyExecutionContext);

            // Assert
            downloaderMock.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<IList<Uri>>()), Times.Never());
        }

        [Test]
        public async Task RunAsync_ExportLinkExtractorReturnsNoLinks_DoesNotDownloadExports()
        {
            // Arrange
            extractorMock.Setup(x => x.ExtractAsync())
                         .ReturnsAsync(new SfExportLinkExtractorResult(true, new List<Uri>()));

            // Act
            await function.RunAsync(dummyTimer, dummyStream, dummyExecutionContext);

            // Assert
            downloaderMock.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<IList<Uri>>()), Times.Never());
        }

        [Test]
        public async Task RunAsync_ConsolidatesExports()
        {
            // Act
            await function.RunAsync(dummyTimer, dummyStream, dummyExecutionContext);

            // Assert
            consolidatorMock.Verify(x => x.Consolidate(It.IsAny<IList<string>>(), It.IsAny<string>()));
        }

        [Test]
        public async Task RunAsync_ExportDownloaderFails_DoesNotConsolidateExports()
        {
            // Arrange
            downloaderMock.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<IList<Uri>>()))
                         .ReturnsAsync(new SfExportDownloaderResult(false));

            // Act
            await function.RunAsync(dummyTimer, dummyStream, dummyExecutionContext);

            // Assert
            consolidatorMock.Verify(x => x.Consolidate(It.IsAny<IList<string>>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public async Task RunAsync_WritesConsolidatedExportToExportStream()
        {
            // Act
            await function.RunAsync(dummyTimer, dummyStream, dummyExecutionContext);

            // Assert
            Assert.That(dummyStream.Length, Is.EqualTo(TestData.Export.Length));
        }
    }
}