using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SfDataBackup.Services.Auth;

namespace SfDataBackup.Services
{
    public class SfService : ISfService
    {
        private ILogger<SfService> logger;
        private IHttpClientFactory httpClientFactory;
        private ISfJwtAuthService authService;
        private SfOptions options;

        public SfService(
            ILogger<SfService> logger,
            IHttpClientFactory httpClientFactory,
            ISfJwtAuthService authService,
            IOptionsSnapshot<SfOptions> optionsProvider
        )
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.authService = authService;
            this.options = optionsProvider.Value;
        }

        public async Task<string> GetPageSourceAsync(string relativeUrl)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);

            var response = await SendRequestToOrgAsync(request);

            return await response.Content.ReadAsStringAsync();
        }

        public Task<Stream> DownloadFileAsync(string relativeUrl)
        {
            throw new NotImplementedException();
        }

        private async Task<HttpResponseMessage> SendRequestToOrgAsync(HttpRequestMessage request)
        {
            // Get access token
            var accessToken = await authService.GetAccessTokenAsync();

            // Create oid/sid cookie header
            var cookieContainer = new CookieContainer();
            var oidCookie = new Cookie("oid", options.OrganisationId);
            var sidCookie = new Cookie("sid", accessToken);
            cookieContainer.Add(options.OrganisationUrl, oidCookie);
            cookieContainer.Add(options.OrganisationUrl, sidCookie);

            // Set cookie header
            var cookieHeader = cookieContainer.GetCookieHeader(options.OrganisationUrl);
            request.Headers.Add("Cookie", cookieHeader);

            var client = httpClientFactory.CreateClient("DefaultClient");
            client.BaseAddress = options.OrganisationUrl;

            return await client.SendAsync(request);
        }
    }
}