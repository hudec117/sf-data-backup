using System;
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
        private const string dummyRelativeUrl = "/services/data";
        private const string dummyAccessToken = "dummyaccesstoken";

        private Mock<HttpMessageHandler> httpMessageHandlerMock;
        private Mock<IHttpClientFactory> httpClientFactoryMock;
        private Mock<ISfJwtAuthService> authServiceMock;

        private SfService service;

        [SetUp]
        public void Setup()
        {
            // Setup mocks
            var logger = new Mock<ILogger<SfService>>();

            httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            // Mock response for GetPageSourceAsync
            var exportPageRequestUrl = new Uri(TestData.Options.OrganisationUrl, dummyRelativeUrl);
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

            httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient("DefaultClient"))
                                 .Returns(new HttpClient(httpMessageHandlerMock.Object));

            authServiceMock = new Mock<ISfJwtAuthService>();
            authServiceMock.Setup(x => x.GetAccessTokenAsync())
                           .ReturnsAsync(dummyAccessToken);

            service = new SfService(logger.Object, httpClientFactoryMock.Object, authServiceMock.Object, TestData.OptionsProvider);
        }

        [Test]
        public async Task GetPageSourceAsync_SendsRequestToOrgWithRelativeUrl()
        {
            // Act
            await service.GetPageSourceAsync(dummyRelativeUrl);

            // Assert
            var expectedRequestUrl = new Uri(TestData.Options.OrganisationUrl, dummyRelativeUrl);
            httpMessageHandlerMock.Protected()
                                  .Verify(
                                      "SendAsync",
                                      Times.Once(),
                                      ItExpr.Is<HttpRequestMessage>(msg => msg.RequestUri == expectedRequestUrl),
                                      ItExpr.IsAny<CancellationToken>()
                                  );
        }

        [Test]
        public async Task GetPageSourceAsync_SendsRequestWithAuthCookies()
        {
            // Act
            await service.GetPageSourceAsync(dummyRelativeUrl);

            // Assert
            var expectedRequestUrl = new Uri(TestData.Options.OrganisationUrl, dummyRelativeUrl);
            httpMessageHandlerMock.Protected()
                                  .Verify(
                                      "SendAsync",
                                      Times.Once(),
                                      ItExpr.Is<HttpRequestMessage>(msg => VerifyRequestHasAuthCookie(msg)),
                                      ItExpr.IsAny<CancellationToken>()
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
        public async Task GetPageSourceAsync_ReturnsSourceOfRequestedPage()
        {
            // Act
            var source = await service.GetPageSourceAsync(dummyRelativeUrl);

            // Assert
            Assert.That(source, Is.EqualTo(TestData.ExportSingleExportAvailablePage));
        }
    }
}