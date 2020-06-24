using System.Threading.Tasks;

namespace SfDataBackup.Services.Auth
{
    public interface ISfAuthService
    {
        Task<string> LoginAsync(string username, string password);

        Task LogoutAsync();
    }
}