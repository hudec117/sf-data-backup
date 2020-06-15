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

            // Extract each ZIP and delete ZIP after extracted.
            foreach (var exportPath in exportPaths)
            {
                // TODO: handle exceptions
                zipFile.ExtractToDirectory(exportPath, tempFolderPath, true);

                fileSystem.File.Delete(exportPath);

                // TODO: how much larger are the extracted contents than archives?
            }

            zipFile.CreateFromDirectory(tempFolderPath, consolidatedExportPath);

            fileSystem.Directory.Delete(tempFolderPath, true);

            return new SfResult(true);
        }
    }
}