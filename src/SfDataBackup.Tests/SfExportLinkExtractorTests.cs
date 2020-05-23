using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using SfDataBackup.Extractors;

namespace SfDataBackup.Tests
{
    public class SfExportLinkExtractorTests
    {
        private Mock<ILogger<SfExportLinkExtractor>> loggerMock;

        private Mock<HttpMessageHandler> httpMessageHandlerMock;
        private Mock<IHttpClientFactory> httpClientFactoryMock;

        private SfExportLinkExtractorConfig dummyExtractorConfig;

        private SfExportLinkExtractor extractor;

        [SetUp]
        public void Setup()
        {
            loggerMock = new Mock<ILogger<SfExportLinkExtractor>>();

            httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                                  .ReturnsAsync(() =>
                                  {
                                      var response = new HttpResponseMessage(HttpStatusCode.OK);
                                      response.Content = new StringContent(SharedData.ExportSingleExportAvailablePage);

                                      return response;
                                  });

            httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient("SalesforceClient"))
                                 .Returns(new HttpClient(httpMessageHandlerMock.Object));

            dummyExtractorConfig = new SfExportLinkExtractorConfig(SharedData.Config)
            {
                ExportServicePath = "/dummy/export/service/path",
                ExportServiceRegex = "<a\\s+href=\"(?'relurl'\\/servlet\\/servlet\\.OrgExport\\?.+?)\""
            };

            extractor = new SfExportLinkExtractor(loggerMock.Object, dummyExtractorConfig, httpClientFactoryMock.Object);
        }

        [Test]
        public async Task ExtractAsync_RequestsExportServicePage()
        {
            // Act
            await extractor.ExtractAsync();

            // Assert
            httpMessageHandlerMock.Protected()
                                  .Verify(
                                      "SendAsync",
                                      Times.Once(),
                                      ItExpr.Is<HttpRequestMessage>(message => message.RequestUri.PathAndQuery == dummyExtractorConfig.ExportServicePath),
                                      ItExpr.IsAny<CancellationToken>()
                                  );
        }

        [Test]
        public async Task ExtractAsync_ResultSuccessIsTrue()
        {
            // Act
            var result = await extractor.ExtractAsync();

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task ExtractAsync_SingleExportAvailable_ReturnsDownloadLink()
        {
            // Act
            var result = await extractor.ExtractAsync();

            // Assert
            var expectedUrl = new Uri(dummyExtractorConfig.OrganisationUrl, "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_1.ZIP&id=0924J000000YpZK");
            Assert.That(result.Links[0], Is.EqualTo(expectedUrl));
        }

        // [Test]
        // public async Task ExtractAsync_MultipleExportsAvailable_ReturnsDownloadLinks()
        // {
        //     // Act
        //     var result = await extractor.ExtractAsync();

        //     // Assert
        //     Assert.That(result.Links.Count, Is.EqualTo(1));
        // }

        [Test]
        public async Task ExtractAsync_MalformedPage_ReturnsNoLinks()
        {
            // Arrange
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                                  .ReturnsAsync(() =>
                                  {
                                      var response = new HttpResponseMessage(HttpStatusCode.OK);
                                      response.Content = new StringContent(SharedData.MalformedPage);

                                      return response;
                                  });

            // Act
            var result = await extractor.ExtractAsync();

            // Assert
            Assert.That(result.Links, Is.Empty);
        }

        [Test]
        public async Task ExtractAsync_NetworkFailure_ResultSuccessIsFalse()
        {
            // Assert
            httpMessageHandlerMock.Protected()
                                  .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                                  .ThrowsAsync(new HttpRequestException());

            // Act
            var result = await extractor.ExtractAsync();

            // Assert
            Assert.That(result.Success, Is.False);
        }
    }
}