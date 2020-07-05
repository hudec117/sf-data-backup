using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SfDataBackup.Consolidators;
using SfDataBackup.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System;

namespace SfDataBackup.Tests
{
    public class ZipFileConsolidatorTests
    {
        private List<string> dummyZipFilePaths;
        private string dummyTempFolderPath;

        private Mock<IZipFile> zipFileMock;
        private Mock<IFile> fileMock;
        private Mock<IDirectory> directoryMock;
        private Mock<IPath> pathMock;
        private Mock<IFileSystem> fileSystemMock;

        private ZipFileConsolidator consolidator;

        [SetUp]
        public void Setup()
        {
            dummyZipFilePaths = new List<string>();
            dummyZipFilePaths.Add("C:\\my\\dummy\\file0.zip");

            var loggerMock = new Mock<ILogger<ZipFileConsolidator>>();

            zipFileMock = new Mock<IZipFile>();

            fileMock = new Mock<IFile>();

            directoryMock = new Mock<IDirectory>();

            pathMock = new Mock<IPath>();
            pathMock.Setup(x => x.Combine(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((p1, p2) => Path.Combine(p1, p2));
            pathMock.Setup(x => x.GetTempPath())
                    .Returns($"C:\\tmp");
            pathMock.Setup(x => x.GetTempFileName())
                    .Returns($"C:\\tmp\\{Guid.NewGuid()}.tmp");

            fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.SetupGet(x => x.File)
                          .Returns(fileMock.Object);
            fileSystemMock.SetupGet(x => x.Directory)
                          .Returns(directoryMock.Object);
            fileSystemMock.SetupGet(x => x.Path)
                          .Returns(pathMock.Object);

            consolidator = new ZipFileConsolidator(loggerMock.Object, zipFileMock.Object, fileSystemMock.Object);
        }

        [Test]
        public void Consolidate_ExtractsEachFile()
        {
            // Act
            consolidator.Consolidate(dummyZipFilePaths);

            // Assert
            foreach (var filePath in dummyZipFilePaths)
                zipFileMock.Verify(x => x.ExtractToDirectory(filePath, It.IsAny<string>(), true));
        }

        [Test]
        public void Consolidate_ExtractionThrowsInvalidDataException_ThrowsConsolidationException()
        {
            // Arrange
            zipFileMock.Setup(x => x.ExtractToDirectory(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                       .Throws<InvalidDataException>();

            // Assert
            Assert.Throws<ConsolidationException>(() =>
            {
                // Act
                consolidator.Consolidate(dummyZipFilePaths);
            });
        }

        [Test]
        public void Consolidate_DeletesFileAfterExtraction()
        {
            // Act
            consolidator.Consolidate(dummyZipFilePaths);

            // Assert
            foreach (var filePath in dummyZipFilePaths)
                fileMock.Verify(x => x.Delete(filePath));
        }

        [Test]
        public void Consolidate_CreatesConsolidatedFile()
        {
            // Act
            consolidator.Consolidate(dummyZipFilePaths);

            // Assert
            zipFileMock.Verify(x => x.CreateFromDirectory(It.IsAny<string>(), It.IsAny<string>()));
        }

        [Test]
        public void Consolidate_ConsolidatedFileCreationThrowsIOException_ThrowsConsolidationException()
        {
            // Arrange
            zipFileMock.Setup(x => x.CreateFromDirectory(It.IsAny<string>(), It.IsAny<string>()))
                       .Throws<IOException>();

            // Assert
            Assert.Throws<ConsolidationException>(() =>
            {
                // Act
                consolidator.Consolidate(dummyZipFilePaths);
            });
        }
    }
}