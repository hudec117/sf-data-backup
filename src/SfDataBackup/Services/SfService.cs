using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
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
        private IFileSystem fileSystem;
        private SfOptions options;

        public SfService(
            ILogger<SfService> logger,
            IHttpClientFactory httpClientFactory,
            ISfJwtAuthService authService,
            IFileSystem fileSystem,
            IOptionsSnapshot<SfOptions> optionsProvider
        )
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
            this.authService = authService;
            this.fileSystem = fileSystem;
            this.options = optionsProvider.Value;
        }

        public async Task<IList<string>> GetExportDownloadLinksAsync()
        {
            var pageResponse = await SendGetRequestToOrgAsync(options.ExportService.Page);
            var pageSource = await pageResponse.Content.ReadAsStringAsync();

            var links = new List<string>();

            var matches = Regex.Matches(pageSource, options.ExportService.Regex, RegexOptions.IgnoreCase);

            // Explicit type is required
            foreach (Match match in matches)
            {
                var relativeUrlGroup = match.Groups["relurl"];
                var relativeUrl = relativeUrlGroup.Value;

                // Remove &amp; from relative URL
                relativeUrl = relativeUrl.Replace("&amp;", "&");

                logger.LogDebug("Extracted {url}", relativeUrl);

                links.Add(relativeUrl);
            }

            return links;
        }

        public async Task<IList<string>> DownloadExportsAsync(string downloadFolderPath, IList<string> relativeExportUrls)
        {
            var downloadedExportPaths = new List<string>();

            for (var i = 0; i < relativeExportUrls.Count; i++)
            {
                var relativeExportUrl = relativeExportUrls[i];

                // Send request for export file
                var response = await SendGetRequestToOrgAsync(relativeExportUrl);
                var responseStream = await response.Content.ReadAsStreamAsync();

                var filePath = fileSystem.Path.Combine(downloadFolderPath, $"export{i + 1}.zip");
                using (var fileStream = fileSystem.File.Create(filePath))
                {
                    await responseStream.CopyToAsync(fileStream);
                }

                downloadedExportPaths.Add(filePath);
            }

            return downloadedExportPaths;
        }

        private async Task<HttpResponseMessage> SendGetRequestToOrgAsync(string relativeUrl)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);

            return await SendRequestToOrgAsync(request);
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

            var client = httpClientFactory.CreateClient("SalesforceClient");

            return await client.SendAsync(request);
        }
    }
}