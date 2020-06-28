using System.Collections.Generic;
using System.Threading.Tasks;

namespace SfDataBackup.Services
{
    public interface ISfService
    {
        Task<IList<string>> GetExportDownloadLinksAsync();

        Task<IList<string>> DownloadExportsAsync(string downloadFolderPath, IList<string> relativeExportUrls);
    }
}