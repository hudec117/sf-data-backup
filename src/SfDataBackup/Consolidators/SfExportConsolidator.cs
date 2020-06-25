using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using SfDataBackup.Abstractions;

namespace SfDataBackup.Consolidators
{
    public class SfExportConsolidator : ISfExportConsolidator
    {
        private ILogger<SfExportConsolidator> logger;
        private IZipFile zipFile;
        private IFileSystem fileSystem;

        private const string tempFolderName = "exports";

        public SfExportConsolidator(ILogger<SfExportConsolidator> logger, IZipFile zipFile, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.zipFile = zipFile;
            this.fileSystem = fileSystem;
        }

        public SfResult Consolidate(IList<string> exportPaths, string consolidatedExportPath)
        {
            var tempFolderPath = Path.Combine(Path.GetDirectoryName(consolidatedExportPath), tempFolderName);
            logger.LogDebug("Using temporary folder {path}", tempFolderPath);

            // Extract each ZIP and delete ZIP after extracted.
            foreach (var exportPath in exportPaths)
            {
                // TODO: handle exceptions
                zipFile.ExtractToDirectory(exportPath, tempFolderPath, true);
                logger.LogDebug("Extracted {path}", exportPath);

                fileSystem.File.Delete(exportPath);
                logger.LogDebug("Deleted {path}", exportPath);

                // TODO: how much larger are the extracted contents than archives?
            }

            zipFile.CreateFromDirectory(tempFolderPath, consolidatedExportPath);
            logger.LogDebug("Consolidated export created at {path}", consolidatedExportPath);

            fileSystem.Directory.Delete(tempFolderPath, true);
            logger.LogDebug("Deleted temporary folder");

            return new SfResult(true);
        }
    }
}