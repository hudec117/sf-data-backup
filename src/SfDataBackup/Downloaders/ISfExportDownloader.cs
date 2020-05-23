using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SfDataBackup.Downloaders
{
    public interface ISfExportDownloader
    {
        Task<SfExportDownloaderResult> DownloadAsync(IList<Uri> exportDownloadLinks);
    }
}