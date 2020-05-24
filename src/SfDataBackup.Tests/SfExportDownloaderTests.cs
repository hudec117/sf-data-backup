using System;
using System.Collections.Generic;
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

namespace SfDataBackup.Tests
{
    public class SfExportDownloaderTests
    {
        private Mock<ILogger<SfExportDownloader>> loggerMock;

        private Mock<HttpMessageHandler> httpMessageHandlerMock;
        private Mock<IHttpClientFactory> httpClientFactoryMock;
        private MockFileSystem fileSystemMock;

        private SfExportDownloaderConfig dummyDownloaderConfig;

        private IList<Uri> singleLinks;
        private IList<Uri> multipleLinks;

        private SfExportDownloader downloader;

        [SetUp]
        public void Setup()
        {
            loggerMock = new Mock<ILogger<SfExportDownloader>>();

            httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                                  .ReturnsAsync(() =>
                                  {
                                      return new HttpResponseMessage(HttpStatusCode.OK)
                                      {
                                          Content = new ByteArrayContent(SharedData.Export)
                                      };
                                  });

            httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient("SalesforceClient"))
                                 .Returns(new HttpClient(httpMessageHandlerMock.Object));

            fileSystemMock = new MockFileSystem();

            singleLinks = new List<Uri>
            {
                new Uri(SharedData.Config.OrganisationUrl, "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_1.ZIP&id=0924J000000YpZK")
            };

            multipleLinks = new List<Uri>
            {
                new Uri(SharedData.Config.OrganisationUrl, "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_1.ZIP&id=0924J000000YpZK"),
                new Uri(SharedData.Config.OrganisationUrl, "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_2.ZIP&id=0924J000000YpZK"),
                new Uri(SharedData.Config.OrganisationUrl, "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_3.ZIP&id=0924J000000YpZK")
            };

            dummyDownloaderConfig = new SfExportDownloaderConfig(SharedData.Config)
            {
                DownloadPath = "exports"
            };

            downloader = new SfExportDownloader(loggerMock.Object, dummyDownloaderConfig, httpClientFactoryMock.Object, fileSystemMock);
        }

        [Test]
        public async Task DownloadAsync_CreatesDownloadPathFolders()
        {
            // Act
            await downloader.DownloadAsync(singleLinks);

            // Assert
            Assert.That(fileSystemMock.AllDirectories, Contains.Item("C:\\exports"));
        }

        [Test]
        public async Task DownloadAsync_ResultSuccessIsTrue()
        {
            // Act
            var result = await downloader.DownloadAsync(multipleLinks);

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task DownloadAsync_SingleExportDownloadLink_SavesResponseContentToFile()
        {
            // Act
            await downloader.DownloadAsync(singleLinks);

            // Assert
            Assert.That(fileSystemMock.AllFiles, Contains.Item("C:\\exports\\export0.zip"));
        }

        [Test]
        public async Task DownloadAsync_SingleExportDownloadLink_RequestsExport()
        {
            // Act
            await downloader.DownloadAsync(singleLinks);

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
            await downloader.DownloadAsync(multipleLinks);

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
            await downloader.DownloadAsync(multipleLinks);

            // Assert
            for (var i = 0; i < multipleLinks.Count; i++)
            {
                Assert.That(fileSystemMock.AllFiles, Contains.Item($"C:\\exports\\export{i}.zip"));
            }
        }

        [Test]
        public async Task DownloadAsync_MultipleExportDownloadLinks_ResultHasLocalPaths()
        {
            // Act
            var result = await downloader.DownloadAsync(multipleLinks);

            // Assert
            for (var i = 0; i < result.Paths.Count; i++)
            {
                var path = result.Paths[i];
                Assert.That(path, Is.EqualTo($"exports\\export{i}.zip"));
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
            await downloader.DownloadAsync(multipleLinks);

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
            await downloader.DownloadAsync(multipleLinks);

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
            var result = await downloader.DownloadAsync(multipleLinks);

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
            var result = await downloader.DownloadAsync(multipleLinks);

            // Assert
            Assert.That(result.Success, Is.False);
        }
    }
}