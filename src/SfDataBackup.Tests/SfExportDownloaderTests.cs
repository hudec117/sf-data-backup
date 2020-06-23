using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SfDataBackup.Downloaders;
using SfDataBackup.Services;
using SfDataBackup.Tests.Data;

namespace SfDataBackup.Tests
{
    public class SfExportDownloaderTests
    {
        private string downloadPath;
        private IList<string> singleLinks;
        private IList<string> multipleLinks;

        private Mock<ISfService> serviceMock;
        private MockFileSystem fileSystemMock;

        private SfExportDownloader downloader;

        [SetUp]
        public void Setup()
        {
            downloadPath = "C:\\myfunctionapp\\myfunction";

            singleLinks = new List<string>
            {
                "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_1.ZIP&id=0924J000000YpZK"
            };

            multipleLinks = new List<string>
            {
                "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_1.ZIP&id=0924J000000YpZK",
                "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_2.ZIP&id=0924J000000YpZK",
                "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_3.ZIP&id=0924J000000YpZK"
            };

            var loggerMock = new Mock<ILogger<SfExportDownloader>>();

            serviceMock = new Mock<ISfService>();
            serviceMock.Setup(x => x.DownloadFileAsync(It.IsAny<string>()))
                       .ReturnsAsync(() => new MemoryStream(TestData.Export));

            fileSystemMock = new MockFileSystem();
            fileSystemMock.AddDirectory(downloadPath);

            downloader = new SfExportDownloader(loggerMock.Object, serviceMock.Object, fileSystemMock);
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
        public async Task DownloadAsync_SingleDownloadLink_DownloadsExport()
        {
            // Act
            await downloader.DownloadAsync(downloadPath, singleLinks);

            // Assert
            serviceMock.Verify(x => x.DownloadFileAsync(singleLinks[0]));
        }

        [Test]
        public async Task DownloadAsync_SingleDownloadLink_DownloadsToFile()
        {
            // Act
            await downloader.DownloadAsync(downloadPath, singleLinks);

            // Assert
            Assert.That(fileSystemMock.AllFiles, Contains.Item(Path.Combine(downloadPath, "export0.zip")));
        }

        [Test]
        public async Task DownloadAsync_MultipleDownloadLinks_DownloadsExports()
        {
            // Act
            await downloader.DownloadAsync(downloadPath, multipleLinks);

            // Assert
            foreach (var link in multipleLinks)
                serviceMock.Verify(x => x.DownloadFileAsync(link));
        }

        [Test]
        public async Task DownloadAsync_MultipleDownloadLinks_DownloadsToFiles()
        {
            // Act
            await downloader.DownloadAsync(downloadPath, multipleLinks);

            // Assert
            for (var i = 0; i < multipleLinks.Count; i++)
                Assert.That(fileSystemMock.AllFiles, Contains.Item(Path.Combine(downloadPath, $"export{i}.zip")));
        }

        [Test]
        public async Task DownloadAsync_MultipleDownloadLinks_ResultHasExportPaths()
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
    }
}