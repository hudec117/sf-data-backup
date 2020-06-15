using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SfDataBackup.Consolidators;
using SfDataBackup.Abstractions;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;

namespace SfDataBackup.Tests
{
    public class SfExportConsolidatorTests
    {
        private List<string> dummyExportPaths;
        private string dummyConsolidatedExportPath;
        private string dummyTempFolderPath;

        private Mock<ILogger<SfExportConsolidator>> loggerMock;
        private Mock<IZipFile> zipFileMock;
        private MockFileSystem fileSystemMock;

        private SfExportConsolidator consolidator;

        [SetUp]
        public void Setup()
        {
            dummyExportPaths = new List<string>();
            dummyExportPaths.Add("C:\\my\\dummy\\export0.zip");
            dummyConsolidatedExportPath = "C:\\my\\dummy\\export.zip";
            dummyTempFolderPath = "C:\\my\\dummy\\exports";

            loggerMock = new Mock<ILogger<SfExportConsolidator>>();

            zipFileMock = new Mock<IZipFile>();

            fileSystemMock = new MockFileSystem();
            foreach (var exportPath in dummyExportPaths)
                fileSystemMock.AddFile(exportPath, new MockFileData(SharedData.Export));

            consolidator = new SfExportConsolidator(loggerMock.Object, zipFileMock.Object, fileSystemMock);
        }

        [Test]
        public void Consolidate_ExtractsEachExportPath()
        {
            // Act
            consolidator.Consolidate(dummyExportPaths, dummyConsolidatedExportPath);

            // Assert
            foreach (var exportPath in dummyExportPaths)
            {
                zipFileMock.Verify(x => x.ExtractToDirectory(exportPath, dummyTempFolderPath, true));
            }
        }

        [Test]
        public void Consolidate_DeletesExportAfterExtraction()
        {
            // Act
            consolidator.Consolidate(dummyExportPaths, dummyConsolidatedExportPath);

            // Assert
            foreach (var exportPath in dummyExportPaths)
                Assert.That(!fileSystemMock.FileExists(exportPath));
        }

        [Test]
        public void Consolidate_CreatesConsolidatedExport()
        {
            // Act
            consolidator.Consolidate(dummyExportPaths, dummyConsolidatedExportPath);

            // Assert
            zipFileMock.Verify(x => x.CreateFromDirectory(dummyTempFolderPath, dummyConsolidatedExportPath));
        }
    }
}