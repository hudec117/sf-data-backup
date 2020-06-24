using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SfDataBackup.WSDL;

namespace SfDataBackup.Services.Auth
{
    public class SfAuthService : ISfAuthService
    {
        private ILogger<SfAuthService> logger;
        private SfOptions options;

        private string sessionId;

        private SfDataBackup.WSDL.SoapClient client;

        public SfAuthService(
            ILogger<SfAuthService> logger,
            IOptionsSnapshot<SfOptions> optionsProvider
        )
        {
            this.logger = logger;
            this.options = optionsProvider.Value;

            client = new SfDataBackup.WSDL.SoapClient();
        }

        public async Task<string> LoginAsync(string username, string password)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
                return sessionId;

            var response = await client.loginAsync(
                new LoginScopeHeader(),
                new CallOptions(),
                username,
                password
            );

            return sessionId = response.result.sessionId;
        }

        public async Task LogoutAsync()
        {
            await client.logoutAsync(
                new SessionHeader
                {
                    sessionId = sessionId
                },
                new CallOptions()
            );

            sessionId = null;
        }
    }
}