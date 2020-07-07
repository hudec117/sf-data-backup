using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
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
        private Mock<ISfService> serviceMock;
        private Mock<IZipFileConsolidator> consolidatorMock;
        private Mock<ICloudBlob> blobMock;
        private Mock<IFile> fileMock;
        private Mock<IFileSystem> fileSystemMock;

        private TimerInfo dummyTimer;
        private ExecutionContext dummyExecutionContext;

        private DownloadWeekly function;

        [SetUp]
        public void Setup()
        {
            var loggerMock = new Mock<ILogger<DownloadWeekly>>();

            // Setup Downloader mock
            var dummyPaths = new List<string>
            {
                "C:\\tmp\\" + Guid.NewGuid()
            };

            serviceMock = new Mock<ISfService>();
            serviceMock.Setup(x => x.GetExportDownloadLinksAsync())
                       .ReturnsAsync(TestData.ExportDownloadLinks);
            serviceMock.Setup(x => x.DownloadExportsAsync(It.IsAny<IList<string>>(), It.IsAny<IProgress<int>>()))
                       .ReturnsAsync(dummyPaths);

            consolidatorMock = new Mock<IZipFileConsolidator>();

            fileMock = new Mock<IFile>();

            fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(x => x.File)
                          .Returns(fileMock.Object);

            blobMock = new Mock<ICloudBlob>();

            var schedule = new DailySchedule();
            var status = new ScheduleStatus();
            dummyTimer = new TimerInfo(schedule, status);

            dummyExecutionContext = new ExecutionContext
            {
                FunctionDirectory = "C:\\myfuncapp\\DownloadWeekly"
            };

            function = new DownloadWeekly(loggerMock.Object, serviceMock.Object, consolidatorMock.Object, fileSystemMock.Object);
        }

        [Test]
        public async Task RunAsync_ExtractsLinks()
        {
            // Act
            await function.RunAsync(dummyTimer, blobMock.Object, dummyExecutionContext);

            // Assert
            serviceMock.Verify(x => x.GetExportDownloadLinksAsync());
        }

        [Test]
        public async Task RunAsync_DownloadsLinks()
        {
            // Act
            await function.RunAsync(dummyTimer, blobMock.Object, dummyExecutionContext);

            // Assert
            serviceMock.Verify(x => x.DownloadExportsAsync(It.IsAny<IList<string>>(), It.IsAny<IProgress<int>>()));
        }

        [Test]
        public void RunAsync_NoLinksToDownload_ThrowsDownloadWeeklyException()
        {
            // Arrange
            serviceMock.Setup(x => x.GetExportDownloadLinksAsync())
                       .ReturnsAsync(new List<string>());

            // Assert
            Assert.ThrowsAsync<DownloadWeeklyException>(async () => {
                // Act
                await function.RunAsync(dummyTimer, blobMock.Object, dummyExecutionContext);
            });
        }

        [Test]
        public async Task RunAsync_ConsolidatesExports()
        {
            // Act
            await function.RunAsync(dummyTimer, blobMock.Object, dummyExecutionContext);

            // Assert
            consolidatorMock.Verify(x => x.Consolidate(It.IsAny<IList<string>>(), It.IsAny<IProgress<int>>()));
        }

        [Test]
        public void RunAsync_ConsolidatorThrowsConsolidationException_ThrowDownloadWeeklyException()
        {
            // Arrange
            consolidatorMock.Setup(x => x.Consolidate(It.IsAny<IList<string>>(), It.IsAny<IProgress<int>>()))
                            .Throws<ConsolidationException>();

            // Assert
            Assert.ThrowsAsync<DownloadWeeklyException>(async () => {
                // Act
                await function.RunAsync(dummyTimer, blobMock.Object, dummyExecutionContext);
            });
        }

        [Test]
        public async Task RunAsync_UploadsConsolidatedExportToBlob()
        {
            // Act
            await function.RunAsync(dummyTimer, blobMock.Object, dummyExecutionContext);

            // Assert
            blobMock.Verify(x => x.UploadFromFileAsync(It.IsAny<string>()));
        }

        [Test]
        public void RunAsync_ConsolidatedExportUploadThrowsStorageException_ThrowsDownloadWeeklyException()
        {
            // Arrange
            blobMock.Setup(x => x.UploadFromFileAsync(It.IsAny<string>()))
                    .Throws<StorageException>();

            // Assert
            Assert.ThrowsAsync<DownloadWeeklyException>(async () => {
                // Act
                await function.RunAsync(dummyTimer, blobMock.Object, dummyExecutionContext);
            });
        }

        [Test]
        public async Task RunAsync_DeletesConsolidatedExport()
        {
            // Arrange
            var consolidatedExportPath = "C:\\tmp\\" + Guid.NewGuid();
            consolidatorMock.Setup(x => x.Consolidate(It.IsAny<List<string>>(), It.IsAny<IProgress<int>>()))
                            .Returns(consolidatedExportPath);

            // Act
            await function.RunAsync(dummyTimer, blobMock.Object, dummyExecutionContext);

            // Assert
            fileMock.Verify(x => x.Delete(consolidatedExportPath));
        }
    }
}