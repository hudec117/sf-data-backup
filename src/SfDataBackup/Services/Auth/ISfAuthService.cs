using System.Threading.Tasks;

namespace SfDataBackup.Services.Auth
{
    public interface ISfAuthService
    {
        Task<string> GetSessionIdAsync(string username, string password);
    }
}