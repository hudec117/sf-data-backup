using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SfDataBackup.Extractors;
using SfDataBackup.Services;
using SfDataBackup.Tests.Data;

namespace SfDataBackup.Tests
{
    public class SfExportLinkExtractorTests
    {
        private Mock<ISfService> serviceMock;

        private SfExportLinkExtractor extractor;

        [SetUp]
        public void Setup()
        {
            var loggerMock = new Mock<ILogger<SfExportLinkExtractor>>();

            serviceMock = new Mock<ISfService>();
            serviceMock.Setup(x => x.GetPageSourceAsync(TestData.Options.ExportService.Page))
                       .ReturnsAsync(TestData.ExportSingleExportAvailablePage);

            extractor = new SfExportLinkExtractor(loggerMock.Object, serviceMock.Object, TestData.OptionsProvider);
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
        public async Task ExtractAsync_GetsExportServicePageSource()
        {
            // Act
            await extractor.ExtractAsync();

            // Assert
            serviceMock.Verify(x => x.GetPageSourceAsync(TestData.Options.ExportService.Page));
        }

        [Test]
        public async Task ExtractAsync_SingleExportAvailable_ReturnsLink()
        {
            // Act
            var result = await extractor.ExtractAsync();

            // Assert
            Assert.That(result.Links[0], Is.EqualTo(TestData.ExtractLink));
        }

        [Test]
        public async Task ExtractAsync_MalformedPage_ReturnsNoLinks()
        {
            // Arrange
            serviceMock.Setup(x => x.GetPageSourceAsync(TestData.Options.ExportService.Page))
                       .ReturnsAsync(TestData.ExportNoneAvailablePage);

            // Act
            var result = await extractor.ExtractAsync();

            // Assert
            Assert.That(result.Links, Is.Empty);
        }
    }
}