using System.IO;
using System.Threading.Tasks;

namespace SfDataBackup.Services
{
    public interface ISfService
    {
        Task<string> GetPageSourceAsync(string relativeUrl);

        Task<Stream> DownloadFileAsync(string relativeUrl);
    }
}