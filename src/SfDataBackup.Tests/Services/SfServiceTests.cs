using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using SfDataBackup.Services;
using SfDataBackup.Services.Auth;
using SfDataBackup.Tests.Data;

namespace SfDataBackup.Tests.Services
{
    public class SfServiceTests
    {
        private const string dummyAccessToken = "dummyaccesstoken";

        private Mock<HttpMessageHandler> httpMessageHandlerMock;
        private Mock<IHttpClientFactory> httpClientFactoryMock;
        private Mock<ISfAuthService> authServiceMock;

        private Mock<IFile> fileMock;
        private Mock<IPath> pathMock;
        private Mock<IFileSystem> fileSystemMock;

        private SfService service;

        [SetUp]
        public void Setup()
        {
            // Setup mocks
            var logger = new Mock<ILogger<SfService>>();

            httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            // Mock response for export page request
            var exportPageRequestUrl = new Uri(TestData.Options.OrganisationUrl, TestData.Options.ExportService.Page);
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>(
                                      "SendAsync",
                                      ItExpr.Is<HttpRequestMessage>(msg => msg.RequestUri == exportPageRequestUrl),
                                      ItExpr.IsAny<CancellationToken>()
                                  )
                                  .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                                  {
                                      Content = new StringContent(TestData.ExportSingleExportAvailablePage)
                                  });

            // Mock response for export request
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>(
                                      "SendAsync",
                                      ItExpr.Is<HttpRequestMessage>(msg => msg.RequestUri.PathAndQuery.Contains("OrgExport")),
                                      ItExpr.IsAny<CancellationToken>()
                                  )
                                  .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                                  {
                                      Content = new ByteArrayContent(TestData.Export)
                                  });

            httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient("SalesforceClient"))
                                 .Returns(() =>
                                 {
                                     var client = new HttpClient(httpMessageHandlerMock.Object);
                                     client.BaseAddress = TestData.Options.OrganisationUrl;

                                     return client;
                                 });

            authServiceMock = new Mock<ISfAuthService>();
            authServiceMock.Setup(x => x.GetSessionIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                           .ReturnsAsync(dummyAccessToken);

            fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.Open(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>()))
                    .Returns(() => new MemoryStream());

            pathMock = new Mock<IPath>();
            pathMock.Setup(x => x.GetTempFileName())
                    .Returns("C:\\tmp\\" + Guid.NewGuid());

            fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(x => x.File)
                          .Returns(fileMock.Object);
            fileSystemMock.Setup(x => x.Path)
                          .Returns(pathMock.Object);

            service = new SfService(
                logger.Object,
                httpClientFactoryMock.Object,
                authServiceMock.Object,
                fileSystemMock.Object,
                TestData.OptionsProvider
            );
        }

        private bool VerifyRequestHasAuthCookie(HttpRequestMessage request)
        {
            var cookiesCollection = request.Headers.GetCookies();

            var cookieHeader = cookiesCollection.First();

            return cookieHeader.Cookies.FirstOrDefault(x => x.Name == "sid" && x.Value == dummyAccessToken) != default;
        }

        [Test]
        public async Task GetExportDownloadLinksAsync_RequestsExportServicePage()
        {
            // Act
            await service.GetExportDownloadLinksAsync();

            // Assert
            var expectedRequestUrl = new Uri(TestData.Options.OrganisationUrl, TestData.Options.ExportService.Page);
            httpMessageHandlerMock.Protected()
                                  .Verify(
                                      "SendAsync",
                                      Times.Once(),
                                      ItExpr.Is<HttpRequestMessage>(req => req.RequestUri == expectedRequestUrl && VerifyRequestHasAuthCookie(req)),
                                      ItExpr.IsAny<CancellationToken>()
                                  );
        }

        [Test]
        public async Task GetExportDownloadLinksAsync_SingleExportAvailable_ReturnsLink()
        {
            // Act
            var links = await service.GetExportDownloadLinksAsync();

            // Assert
            Assert.That(links[0], Is.EqualTo(TestData.ExportDownloadLinks[0]));
        }

        [Test]
        public async Task GetExportDownloadLinksAsync_NoExportsAvailable_ReturnsNoLinks()
        {
            // Arrange
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>(
                                      "SendAsync",
                                      ItExpr.IsAny<HttpRequestMessage>(),
                                      ItExpr.IsAny<CancellationToken>()
                                  )
                                  .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                                  {
                                      Content = new StringContent(TestData.ExportNoneAvailablePage)
                                  });

            // Act
            var links = await service.GetExportDownloadLinksAsync();

            // Assert
            Assert.That(links, Is.Empty);
        }

        [Test]
        public async Task DownloadExportsAsync_SingleLink_RequestsSingleFile()
        {
            // Arrange
            var singleLinks = new List<string>
            {
                TestData.ExportDownloadLinks[0]
            };

            // Act
            await service.DownloadExportsAsync(singleLinks);

            // Assert
            var expectedRequestUrl = new Uri(TestData.Options.OrganisationUrl, singleLinks[0]);
            httpMessageHandlerMock.Protected()
                                  .Verify(
                                      "SendAsync",
                                      Times.Once(),
                                      ItExpr.Is<HttpRequestMessage>(req => req.RequestUri == expectedRequestUrl && VerifyRequestHasAuthCookie(req)),
                                      ItExpr.IsAny<CancellationToken>()
                                  );
        }

        [Test]
        public async Task DownloadExportsAsync_MultipleLinks_RequestsMultipleFiles()
        {
            // Act
            await service.DownloadExportsAsync(TestData.ExportDownloadLinks);

            // Assert
            foreach (var exportDownloadLink in TestData.ExportDownloadLinks)
            {
                var expectedRequestUrl = new Uri(TestData.Options.OrganisationUrl, exportDownloadLink);
                httpMessageHandlerMock.Protected()
                                      .Verify(
                                          "SendAsync",
                                          Times.Once(),
                                          ItExpr.Is<HttpRequestMessage>(req => req.RequestUri == expectedRequestUrl && VerifyRequestHasAuthCookie(req)),
                                          ItExpr.IsAny<CancellationToken>()
                                      );
            }
        }

        [Test]
        public async Task DownloadExportsAsync_SavesResponseToTemporaryFile()
        {
            // Arrange
            var dummyTmpFilePath = "C:\\tmp\\" + Guid.NewGuid();
            pathMock.Setup(x => x.GetTempFileName())
                    .Returns(dummyTmpFilePath);

            // Act
            await service.DownloadExportsAsync(TestData.ExportDownloadLinks);

            // Assert
            fileMock.Verify(
                x => x.Open(dummyTmpFilePath, FileMode.OpenOrCreate, FileAccess.Write),
                Times.Exactly(TestData.ExportDownloadLinks.Count)
            );
        }

        [Test]
        public async Task DownloadExportsAsync_MultipleLinks_ReturnsMultipleFilePaths()
        {
            // Act
            var downloadedExportFilePaths = await service.DownloadExportsAsync(TestData.ExportDownloadLinks);

            // Assert
            Assert.That(downloadedExportFilePaths.Count, Is.EqualTo(TestData.ExportDownloadLinks.Count));
        }
    }
}