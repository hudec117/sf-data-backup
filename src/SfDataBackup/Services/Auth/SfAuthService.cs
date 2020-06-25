using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SfDataBackup.WSDL;

namespace SfDataBackup.Services.Auth
{
    public class SfAuthService : ISfAuthService, IDisposable
    {
        private ILogger<SfAuthService> logger;

        private SfDataBackup.WSDL.SoapClient logoutClient;
        private string sessionId;

        public SfAuthService(ILogger<SfAuthService> logger)
        {
            this.logger = logger;
        }

        public async Task<string> GetSessionIdAsync(string username, string password)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                logger.LogDebug("Already logged in, returning session ID.");
                return sessionId;
            }

            var loginClient = new SfDataBackup.WSDL.SoapClient();
            var response = await loginClient.loginAsync(
                new LoginScopeHeader(),
                new CallOptions(),
                username,
                password
            );

            logoutClient = new SfDataBackup.WSDL.SoapClient();
            logoutClient.Endpoint.Address = new EndpointAddress(response.result.serverUrl);

            logger.LogDebug("Logged in successfully");

            return sessionId = response.result.sessionId;
        }

        public void Dispose()
        {
            logoutClient?.logout(
                new SessionHeader
                {
                    sessionId = sessionId
                },
                new CallOptions()
            );

            sessionId = null;

            logger.LogDebug("Logged out successfully");
        }
    }
}