using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SfDataBackup.Downloaders;
using SfDataBackup.Extractors;

namespace SfDataBackup.Tests
{
    public class DownloadExportTests
    {
        private Mock<ILogger<DownloadExport>> loggerMock;
        private Mock<ISfExportLinkExtractor> extractorMock;
        private Mock<ISfExportDownloader> downloaderMock;

        private HttpRequest dummyRequest;
        private ExecutionContext dummyExecutionContext;

        private DownloadExport function;

        [SetUp]
        public void Setup()
        {
            loggerMock = new Mock<ILogger<DownloadExport>>();

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

            dummyRequest = new DefaultHttpRequest(new DefaultHttpContext());

            dummyExecutionContext = new ExecutionContext
            {
                FunctionDirectory = "C:\\myfuncapp\\DownloadWeekly"
            };

            function = new DownloadExport(loggerMock.Object, extractorMock.Object, downloaderMock.Object);
        }

        [Test]
        public async Task RunAsync_ExtractsLinks()
        {
            // Act
            await function.RunAsync(dummyRequest, dummyExecutionContext);

            // Assert
            extractorMock.Verify(x => x.ExtractAsync());
        }

        [Test]
        public async Task RunAsync_ExtractLinksFails_ReturnsStatusCodeResultWith500()
        {
            // Arrange
            extractorMock.Setup(x => x.ExtractAsync())
                         .ReturnsAsync(new SfExportLinkExtractorResult(false));

            // Act
            var result = await function.RunAsync(dummyRequest, dummyExecutionContext);

            // Assert
            Assert.That(result, Is.TypeOf<StatusCodeResult>());
            Assert.That((result as StatusCodeResult).StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task RunAsync_NoLinksExtracted_ReturnsNotFoundResult()
        {
            // Arrange
            extractorMock.Setup(x => x.ExtractAsync())
                         .ReturnsAsync(new SfExportLinkExtractorResult(true, new List<Uri>()));

            // Act
            var result = await function.RunAsync(dummyRequest, dummyExecutionContext);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task RunAsync_DownloadsLinks()
        {
            // Act
            await function.RunAsync(dummyRequest, dummyExecutionContext);

            // Assert
            downloaderMock.Verify(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<IList<Uri>>()));
        }

        [Test]
        public async Task RunAsync_DownloaderFails_ReturnsStatusCodeResultWith500()
        {
            // Arrange
            downloaderMock.Setup(x => x.DownloadAsync(It.IsAny<string>(), It.IsAny<IList<Uri>>()))
                         .ReturnsAsync(new SfExportDownloaderResult(false));

            // Act
            var result = await function.RunAsync(dummyRequest, dummyExecutionContext);

            // Assert
            Assert.That(result, Is.TypeOf<StatusCodeResult>());
            Assert.That((result as StatusCodeResult).StatusCode, Is.EqualTo(500));
        }
    }
}