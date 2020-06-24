using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
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
        private const string dummyDownloadFolderPath = "C:\\dummy";
        private const string dummyAccessToken = "dummyaccesstoken";

        private Mock<HttpMessageHandler> httpMessageHandlerMock;
        private Mock<IHttpClientFactory> httpClientFactoryMock;
        private Mock<ISfJwtAuthService> authServiceMock;
        private MockFileSystem fileSystemMock;

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

            authServiceMock = new Mock<ISfJwtAuthService>();
            authServiceMock.Setup(x => x.GetAccessTokenAsync())
                           .ReturnsAsync(dummyAccessToken);

            fileSystemMock = new MockFileSystem();
            fileSystemMock.AddDirectory(dummyDownloadFolderPath);

            service = new SfService(
                logger.Object,
                httpClientFactoryMock.Object,
                authServiceMock.Object,
                fileSystemMock,
                TestData.OptionsProvider
            );
        }

        private bool VerifyRequestHasAuthCookie(HttpRequestMessage request)
        {
            var cookiesCollection = request.Headers.GetCookies();

            var cookieHeader = cookiesCollection.First();

            var hasOidCookie = cookieHeader.Cookies.FirstOrDefault(x => x.Name == "oid" && x.Value == TestData.Options.OrganisationId) != default;
            var hasSidCookie = cookieHeader.Cookies.FirstOrDefault(x => x.Name == "sid" && x.Value == dummyAccessToken) != default;

            return hasOidCookie && hasSidCookie;
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
            await service.DownloadExportsAsync(dummyDownloadFolderPath, singleLinks);

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
            await service.DownloadExportsAsync(dummyDownloadFolderPath, TestData.ExportDownloadLinks);

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
        public async Task DownloadExportsAsync_MultipleLinks_ReturnsMultipleFilePaths()
        {
            // Act
            var downloadedExportFilePaths = await service.DownloadExportsAsync(dummyDownloadFolderPath, TestData.ExportDownloadLinks);

            // Assert
            for (var i = 0; i < TestData.ExportDownloadLinks.Count; i++)
            {
                var path = downloadedExportFilePaths[i];
                var expectedPath = Path.Combine(dummyDownloadFolderPath, $"export{i + 1}.zip");
                Assert.That(path, Is.EqualTo(expectedPath));
            }
        }
    }
}