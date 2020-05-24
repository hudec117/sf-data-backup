using System.Collections.Generic;

namespace SfDataBackup.Downloaders
{
    public class SfExportDownloaderResult : SfResult
    {
        public IList<string> Paths { get; set; }

        public SfExportDownloaderResult(bool success) : base(success)
        {
        }

        public SfExportDownloaderResult(bool success, IList<string> paths) : base(success)
        {
            Paths = paths;
        }
    }
}