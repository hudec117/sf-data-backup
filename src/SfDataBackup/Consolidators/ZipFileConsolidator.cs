using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using SfDataBackup.Abstractions;

namespace SfDataBackup.Consolidators
{
    public class ZipFileConsolidator : IZipFileConsolidator
    {
        public const string TempFolderName = "exports";

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
            var tempFolderPath = Path.Combine(Path.GetDirectoryName(outputZipFilePath), TempFolderName);
            logger.LogDebug("Using temporary folder {path}", tempFolderPath);

            // Extract each ZIP and delete ZIP after extracted.
            foreach (var exportPath in zipFilePaths)
            {
                try
                {
                    zipFile.ExtractToDirectory(exportPath, tempFolderPath, true);
                    logger.LogDebug("Extracted {path}", exportPath);
                }
                catch (InvalidDataException exception)
                {
                    var message = $"Failed to extract {exportPath}, caused by file curruption or unsupported compression method.";
                    logger.LogDebug(message);

                    throw new ConsolidationException(message, exception);
                }

                try
                {
                    fileSystem.File.Delete(exportPath);
                    logger.LogDebug("Deleted {path}", exportPath);
                }
                catch (IOException exception)
                {
                    var message = $"Failed to delete {exportPath} after extracting, the file may be in use or open.";
                    logger.LogDebug(message);

                    throw new ConsolidationException(message, exception);
                }

                // TODO: how much larger are the extracted contents than archives?
            }

            zipFile.CreateFromDirectory(tempFolderPath, outputZipFilePath);
            logger.LogDebug("Consolidated export created at {path}", outputZipFilePath);

            fileSystem.Directory.Delete(tempFolderPath, true);
            logger.LogDebug("Deleted temporary folder");
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