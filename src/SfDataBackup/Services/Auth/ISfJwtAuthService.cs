using System.Threading.Tasks;

namespace SfDataBackup.Services.Auth
{
    public interface ISfJwtAuthService
    {
        Task<string> GetAccessTokenAsync();
    }
}