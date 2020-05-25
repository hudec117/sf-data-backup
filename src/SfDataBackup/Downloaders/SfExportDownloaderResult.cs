using System.Collections.Generic;

namespace SfDataBackup.Downloaders
{
    public class SfExportDownloaderResult : SfResult
    {
        public IList<string> ExportPaths { get; set; }

        public SfExportDownloaderResult(bool success) : base(success)
        {
        }

        public SfExportDownloaderResult(bool success, IList<string> exportPaths) : base(success)
        {
            ExportPaths = exportPaths;
        }
    }
}