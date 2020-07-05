using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using SfDataBackup.Abstractions;

namespace SfDataBackup.Consolidators
{
    public class ZipFileConsolidator : IZipFileConsolidator
    {
        private ILogger<ZipFileConsolidator> logger;
        private IZipFile zipFile;
        private IFileSystem fileSystem;

        public ZipFileConsolidator(ILogger<ZipFileConsolidator> logger, IZipFile zipFile, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.zipFile = zipFile;
            this.fileSystem = fileSystem;
        }

        public string Consolidate(IList<string> zipFilePaths)
        {
            var tempConsolidationPath = GetTempConsolidationPath();
            logger.LogDebug("Using temporary folder {path}", tempConsolidationPath);

            // Extract each ZIP and delete ZIP after extracted.
            foreach (var zipFilePath in zipFilePaths)
            {
                try
                {
                    zipFile.ExtractToDirectory(zipFilePath, tempConsolidationPath, true);
                    logger.LogDebug("Extracted {path}", zipFilePath);
                }
                catch (InvalidDataException exception)
                {
                    // Catch in case a ZIP file or an entry inside it is corrupted.
                    throw ConsolidationExceptionWithMessage(
                        $"Failed to extract {zipFilePath}, caused by file curruption or unsupported compression method.",
                        exception
                    );
                }

                fileSystem.File.Delete(zipFilePath);
                logger.LogDebug("Deleted {path}", zipFilePath);

                // TODO: how much larger are the extracted contents than archives?
            }

            var consolidatedZipFilePath = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), $"{Guid.NewGuid()}.tmp");

            try
            {
                zipFile.CreateFromDirectory(tempConsolidationPath, consolidatedZipFilePath);
                logger.LogDebug("Consolidated ZIP file created at {path}", consolidatedZipFilePath);
            }
            catch (IOException exception)
            {
                // Catch in case a file cannot be opened.
                throw ConsolidationExceptionWithMessage(
                    "Failed to create consolidated ZIP file, a file could not be opened.",
                    exception
                );
            }

            fileSystem.Directory.Delete(tempConsolidationPath, true);
            logger.LogDebug("Deleted temporary folder");

            return consolidatedZipFilePath;
        }

        private string GetTempConsolidationPath()
        {
            var tempFolderPath = fileSystem.Path.GetTempPath();
            var tempFolderName = Guid.NewGuid().ToString();

            return fileSystem.Path.Combine(tempFolderPath, tempFolderName);
        }

        private ConsolidationException ConsolidationExceptionWithMessage(string message, Exception exception)
        {
            logger.LogDebug(message);
            return new ConsolidationException(message, exception);
        }
    }

    [System.Serializable]
    public class ConsolidationException : System.Exception
    {
        public ConsolidationException() { }

        public ConsolidationException(string message) : base(message) { }

        public ConsolidationException(string message, System.Exception inner) : base(message, inner) { }

        protected ConsolidationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context
        ) : base(info, context) { }
    }
}