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
using SfDataBackup.Services;
using SfDataBackup.Tests.Data;

namespace SfDataBackup.Tests
{
    public class DownloadWeeklyTests
    {
        private Mock<ILogger<DownloadWeekly>> loggerMock;
        private Mock<ISfService> serviceMock;
        private Mock<IZipFileConsolidator> consolidatorMock;
        private MockFileSystem fileSystemMock;

        private TimerInfo dummyTimer;
        private MemoryStream dummyStream;
        private ExecutionContext dummyExecutionContext;

        private DownloadWeekly function;

        [SetUp]
        public void Setup()
        {
            loggerMock = new Mock<ILogger<DownloadWeekly>>();

            // Setup Downloader mock
            var dummyPaths = new List<string>
            {
                "exports/mysfexport.zip"
            };

            serviceMock = new Mock<ISfService>();
            serviceMock.Setup(x => x.GetExportDownloadLinksAsync())
                       .ReturnsAsync(TestData.ExportDownloadLinks);
            serviceMock.Setup(x => x.DownloadExportsAsync(It.IsAny<string>(), It.IsAny<IList<string>>()))
                       .ReturnsAsync(dummyPaths);

            consolidatorMock = new Mock<IZipFileConsolidator>();
            consolidatorMock.Setup(x => x.Consolidate(It.IsAny<IList<string>>(), It.IsAny<string>()))
                            .Callback(() =>
                            {
                                var fileToAdd = Path.Combine(dummyExecutionContext.FunctionDirectory, "export.zip");
                                fileSystemMock.AddFile(fileToAdd, new MockFileData(TestData.Export));
                            });

            fileSystemMock = new MockFileSystem();

            var schedule = new DailySchedule();
            var status = new ScheduleStatus();
            dummyTimer = new TimerInfo(schedule, status);

            dummyStream = new MemoryStream();

            dummyExecutionContext = new ExecutionContext
            {
                FunctionDirectory = "C:\\myfuncapp\\DownloadWeekly"
            };

            function = new DownloadWeekly(loggerMock.Object, serviceMock.Object, consolidatorMock.Object, fileSystemMock);
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
            serviceMock.Verify(x => x.GetExportDownloadLinksAsync());
        }

        [Test]
        public async Task RunAsync_DownloadsLinks()
        {
            // Act
            await function.RunAsync(dummyTimer, dummyStream, dummyExecutionContext);

            // Assert
            serviceMock.Verify(x => x.DownloadExportsAsync(It.IsAny<string>(), It.IsAny<IList<string>>()));
        }

        [Test]
        public async Task RunAsync_NoLinksToDownload_DoesNotDownloadExports()
        {
            // Arrange
            serviceMock.Setup(x => x.GetExportDownloadLinksAsync())
                       .ReturnsAsync(new List<string>());

            // Act
            await function.RunAsync(dummyTimer, dummyStream, dummyExecutionContext);

            // Assert
            serviceMock.Verify(x => x.DownloadExportsAsync(It.IsAny<string>(), It.IsAny<IList<string>>()), Times.Never());
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
        public async Task RunAsync_WritesConsolidatedExportToExportStream()
        {
            // Act
            await function.RunAsync(dummyTimer, dummyStream, dummyExecutionContext);

            // Assert
            Assert.That(dummyStream.Length, Is.EqualTo(TestData.Export.Length));
        }
    }
}