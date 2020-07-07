using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public string Consolidate(IList<string> zipFilePaths, IProgress<int> consolidateProgress = null)
        {
            var tempConsolidationPath = GetTempConsolidationPath();
            logger.LogDebug("Using temporary folder {path}", tempConsolidationPath);

            // Extract each ZIP and delete it after.
            for (var i = 0; i < zipFilePaths.Count; i++)
            {
                var zipFilePath = zipFilePaths[i];

                try
                {
                    logger.LogDebug("Starting extraction for {path}", zipFilePath);

                    var stopwatch = Stopwatch.StartNew();
                    zipFile.ExtractToDirectory(zipFilePath, tempConsolidationPath, true);
                    stopwatch.Stop();

                    logger.LogDebug("Extracted {path} in {seconds} seconds", zipFilePath, (int)stopwatch.Elapsed.TotalSeconds);
                }
                catch (InvalidDataException exception)
                {
                    // Catch in case a ZIP file or an entry inside it is corrupted.
                    throw ConsolidationExceptionWithMessage(
                        $"Failed to extract {zipFilePath}, caused by file curruption or unsupported compression method.",
                        exception
                    );
                }

                consolidateProgress?.Report(i + 1);

                fileSystem.File.Delete(zipFilePath);
                logger.LogDebug("Deleted {path}", zipFilePath);
            }

            var consolidatedZipFilePath = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), $"{Guid.NewGuid()}.tmp");

            try
            {
                logger.LogDebug("Creating consolidated ZIP file...", consolidatedZipFilePath);

                var stopwatch = Stopwatch.StartNew();
                zipFile.CreateFromDirectory(tempConsolidationPath, consolidatedZipFilePath);
                stopwatch.Stop();

                logger.LogDebug("Consolidated ZIP file created at {path} in {seconds} seconds", consolidatedZipFilePath, (int)stopwatch.Elapsed.TotalSeconds);
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