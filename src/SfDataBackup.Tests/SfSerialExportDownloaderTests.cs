using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using SfDataBackup.Downloaders;
using SfDataBackup.Tests.Data;

namespace SfDataBackup.Tests
{
    public class SfSerialExportDownloaderTests
    {
        private string downloadPath;
        private IList<Uri> singleLinks;
        private IList<Uri> multipleLinks;

        private Mock<ILogger<SfSerialExportDownloader>> loggerMock;

        private Mock<HttpMessageHandler> httpMessageHandlerMock;
        private Mock<IHttpClientFactory> httpClientFactoryMock;
        private MockFileSystem fileSystemMock;

        private SfSerialExportDownloader downloader;

        [SetUp]
        public void Setup()
        {
            downloadPath = "C:\\myfunctionapp\\myfunction";

            singleLinks = new List<Uri>
            {
                new Uri(TestData.Config.OrganisationUrl, "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_1.ZIP&id=0924J000000YpZK")
            };

            multipleLinks = new List<Uri>
            {
                new Uri(TestData.Config.OrganisationUrl, "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_1.ZIP&id=0924J000000YpZK"),
                new Uri(TestData.Config.OrganisationUrl, "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_2.ZIP&id=0924J000000YpZK"),
                new Uri(TestData.Config.OrganisationUrl, "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_3.ZIP&id=0924J000000YpZK")
            };

            loggerMock = new Mock<ILogger<SfSerialExportDownloader>>();

            httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                                  .ReturnsAsync(() =>
                                  {
                                      return new HttpResponseMessage(HttpStatusCode.OK)
                                      {
                                          Content = new ByteArrayContent(TestData.Export)
                                      };
                                  });

            httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient("DefaultClient"))
                                 .Returns(new HttpClient(httpMessageHandlerMock.Object));

            fileSystemMock = new MockFileSystem();
            fileSystemMock.AddDirectory(downloadPath);

            downloader = new SfSerialExportDownloader(loggerMock.Object, httpClientFactoryMock.Object, fileSystemMock, TestData.Config);
            downloader.AccessToken = "dummyaccesstoken";
        }

        [Test]
        public async Task DownloadAsync_ResultSuccessIsTrue()
        {
            // Act
            var result = await downloader.DownloadAsync(downloadPath, multipleLinks);

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task DownloadAsync_SingleExportDownloadLink_SavesResponseContentToFile()
        {
            // Act
            await downloader.DownloadAsync(downloadPath, singleLinks);

            // Assert
            Assert.That(fileSystemMock.AllFiles, Contains.Item(Path.Combine(downloadPath, "export0.zip")));
        }

        [Test]
        public async Task DownloadAsync_SingleExportDownloadLink_RequestsExport()
        {
            // Act
            await downloader.DownloadAsync(downloadPath, singleLinks);

            // Assert
            httpMessageHandlerMock.Protected()
                                  .Verify(
                                      "SendAsync",
                                      Times.Once(),
                                      ItExpr.Is<HttpRequestMessage>(message => message.RequestUri == singleLinks[0]),
                                      ItExpr.IsAny<CancellationToken>()
                                  );
        }

        [Test]
        public async Task DownloadAsync_MultipleExportDownloadLinks_RequestsExports()
        {
            // Act
            await downloader.DownloadAsync(downloadPath, multipleLinks);

            // Assert
            foreach (var link in multipleLinks)
            {
                httpMessageHandlerMock.Protected()
                                      .Verify(
                                          "SendAsync",
                                          Times.Once(),
                                          ItExpr.Is<HttpRequestMessage>(message => message.RequestUri == link),
                                          ItExpr.IsAny<CancellationToken>()
                                      );
            }
        }

        [Test]
        public async Task DownloadAsync_MultipleExportDownloadLinks_SavesResponseContentToFiles()
        {
            // Act
            await downloader.DownloadAsync(downloadPath, multipleLinks);

            // Assert
            for (var i = 0; i < multipleLinks.Count; i++)
            {
                Assert.That(fileSystemMock.AllFiles, Contains.Item(Path.Combine(downloadPath, $"export{i}.zip")));
            }
        }

        [Test]
        public async Task DownloadAsync_MultipleExportDownloadLinks_ResultHasExportPaths()
        {
            // Act
            var result = await downloader.DownloadAsync(downloadPath, multipleLinks);

            // Assert
            for (var i = 0; i < result.ExportPaths.Count; i++)
            {
                var path = result.ExportPaths[i];
                Assert.That(path, Is.EqualTo(Path.Combine(downloadPath, $"export{i}.zip")));
            }
        }

        [Test]
        public async Task DownloadAsync_UnsuccessfulResponse_DoesNotCreateAnyFiles()
        {
            // Assert
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                                  .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            await downloader.DownloadAsync(downloadPath, multipleLinks);

            // Assert
            Assert.That(fileSystemMock.AllFiles, Is.Empty);
        }

        [Test]
        public async Task DownloadAsync_UnsuccessfulResponse_DoesNotAttemptToDownloadMoreExports()
        {
            // Assert
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                                  .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            await downloader.DownloadAsync(downloadPath, multipleLinks);

            // Assert
            httpMessageHandlerMock.Protected()
                                  .Verify(
                                      "SendAsync",
                                      Times.Once(),
                                      ItExpr.IsAny<HttpRequestMessage>(),
                                      ItExpr.IsAny<CancellationToken>()
                                  );
        }

        [Test]
        public async Task DownloadAsync_HttpClientThrowsException_ResultSuccessIsFalse()
        {
            // Assert
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                                  .ThrowsAsync(new HttpRequestException());

            // Act
            var result = await downloader.DownloadAsync(downloadPath, multipleLinks);

            // Assert
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task DownloadAsync_UnsuccessfulResponse_ResultSuccessIsFalse()
        {
            // Assert
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                                  .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var result = await downloader.DownloadAsync(downloadPath, multipleLinks);

            // Assert
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void DownloadAsync_NoAccessToken_ThrowsInvalidOperationException()
        {
            // Arrange
            downloader.AccessToken = null;

            // Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                // Act
                await downloader.DownloadAsync(downloadPath, multipleLinks);
            });
        }
    }
}