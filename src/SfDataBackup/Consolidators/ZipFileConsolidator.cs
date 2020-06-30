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
        public const string TempFolderName = "consolidate";

        private ILogger<ZipFileConsolidator> logger;
        private IZipFile zipFile;
        private IFileSystem fileSystem;

        public ZipFileConsolidator(ILogger<ZipFileConsolidator> logger, IZipFile zipFile, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.zipFile = zipFile;
            this.fileSystem = fileSystem;
        }

        public void Consolidate(IList<string> zipFilePaths, string outputZipFilePath)
        {
            var tempFolderPath = fileSystem.Path.Combine(Path.GetDirectoryName(outputZipFilePath), TempFolderName);
            logger.LogDebug("Using temporary folder {path}", tempFolderPath);

            // Extract each ZIP and delete ZIP after extracted.
            foreach (var zipFilePath in zipFilePaths)
            {
                try
                {
                    zipFile.ExtractToDirectory(zipFilePath, tempFolderPath, true);
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

                try
                {
                    fileSystem.File.Delete(zipFilePath);
                    logger.LogDebug("Deleted {path}", zipFilePath);
                }
                catch (IOException exception)
                {
                    // Catch in case a ZIP file path cannot be deleted.
                    throw ConsolidationExceptionWithMessage(
                        $"Failed to delete {zipFilePath} after extracting, the file may be in use.",
                        exception
                    );
                }

                // TODO: how much larger are the extracted contents than archives?
            }

            try
            {
                zipFile.CreateFromDirectory(tempFolderPath, outputZipFilePath);
                logger.LogDebug("Consolidated ZIP file created at {path}", outputZipFilePath);
            }
            catch (IOException exception)
            {
                // Catch in case a file cannot be opened.
                throw ConsolidationExceptionWithMessage(
                    "Failed to create consolidated ZIP file, a file could not be opened.",
                    exception
                );
            }

            try
            {
                fileSystem.Directory.Delete(tempFolderPath, true);
                logger.LogDebug("Deleted temporary folder");
            }
            catch (IOException exception)
            {
                // Catch in case the temporary folder cannot be deleted.
                throw ConsolidationExceptionWithMessage(
                    $"Failed delete temporary consolidation folder.",
                    exception
                );
            }
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