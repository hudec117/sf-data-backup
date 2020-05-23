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
                                  .ReturnsAsync(new HttpResponseMessage
                                  {
                                      StatusCode = HttpStatusCode.OK
                                  });

            httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient("Default"))
                                 .Returns(new HttpClient(httpMessageHandlerMock.Object));

            dummyExtractorConfig = new SfExportLinkExtractorConfig(SharedData.Config)
            {
                ExportServicePath = "/dummy/export/service/path",
                ExportServiceRegex = "dummyexportregex"
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
        public async Task ExtractAsync_RequestsExportServicePageWithCookie()
        {
            // Act
            await extractor.ExtractAsync();

            // Assert
            httpMessageHandlerMock.Protected()
                                  .Verify(
                                      "SendAsync",
                                      Times.Once(),
                                      ItExpr.Is<HttpRequestMessage>(message => message.Headers.Contains("Cookie")),
                                      ItExpr.IsAny<CancellationToken>()
                                  );
        }
    }
}