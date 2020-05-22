using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SfDataBackup.Extractors;

namespace SfDataBackup.Tests
{
    public class SfExportLinkExtractorTests
    {
        private Mock<ILogger<SfExportLinkExtractor>> loggerMock;
        private Mock<IHttpClientFactory> httpClientFactoryMock;

        private SfExportLinkExtractorConfig dummyConfig;

        private SfExportLinkExtractor extractor;

        [SetUp]
        public void Setup()
        {
            loggerMock = new Mock<ILogger<SfExportLinkExtractor>>();
            httpClientFactoryMock = new Mock<IHttpClientFactory>();

            dummyConfig = new SfExportLinkExtractorConfig
            {
                ServerUrl = new Uri("https://abcd123.my.salesforce.com"),
                AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
                OrganisationId = "00D4J000000CuzU"
            };

            extractor = new SfExportLinkExtractor(loggerMock.Object, dummyConfig, httpClientFactoryMock.Object);
        }

        [Test]
        public async Task ExtractAsync_RequestsExportServicePage()
        {
            // Act
            await extractor.ExtractAsync();

            // Assert
        }
    }
}