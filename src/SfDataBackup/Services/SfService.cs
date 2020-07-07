using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private ISfAuthService authService;
        private IFileSystem fileSystem;
        private SfOptions options;

        public SfService(
            ILogger<SfService> logger,
            IHttpClientFactory httpClientFactory,
            ISfAuthService authService,
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
            logger.LogDebug("Downloaded Weekly Export Service page source");

            var links = new List<string>();

            var matches = Regex.Matches(pageSource, options.ExportService.Regex, RegexOptions.IgnoreCase);

            // Explicit type is required
            foreach (Match match in matches)
            {
                var relativeUrlGroup = match.Groups["relurl"];
                var relativeUrl = relativeUrlGroup.Value;

                // Remove &amp; from relative URL
                relativeUrl = relativeUrl.Replace("&amp;", "&");

                links.Add(relativeUrl);

                logger.LogDebug("Extracted link: {url}", relativeUrl);
            }

            logger.LogDebug("Found {number} link(s)", links.Count);

            return links;
        }

        public async Task<IList<string>> DownloadExportsAsync(IList<string> relativeExportUrls, IProgress<int> downloadProgress = null)
        {
            var downloadedExportPaths = new List<string>();

            for (var i = 0; i < relativeExportUrls.Count; i++)
            {
                var relativeExportUrl = relativeExportUrls[i];

                logger.LogDebug("Starting download for export {url}", relativeExportUrl);

                var stopwatch = Stopwatch.StartNew();

                // Send request for export file
                var response = await SendGetRequestToOrgAsync(relativeExportUrl);
                var responseStream = await response.Content.ReadAsStreamAsync();

                // Saves response to file
                var filePath = fileSystem.Path.GetTempFileName();
                using (var fileStream = fileSystem.File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    await responseStream.CopyToAsync(fileStream);
                }

                stopwatch.Stop();

                downloadedExportPaths.Add(filePath);

                downloadProgress?.Report(i + 1);

                logger.LogDebug("Downloaded export to {path} in {seconds} seconds", filePath, (int)stopwatch.Elapsed.TotalSeconds);
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
            // Get session ID
            var sessionId = await authService.GetSessionIdAsync(options.Username, options.Password);

            // Create oid/sid cookie header
            var cookieContainer = new CookieContainer();
            var sidCookie = new Cookie("sid", sessionId);
            cookieContainer.Add(options.OrganisationUrl, sidCookie);

            // Set cookie header
            var cookieHeader = cookieContainer.GetCookieHeader(options.OrganisationUrl);
            request.Headers.Add("Cookie", cookieHeader);

            var client = httpClientFactory.CreateClient("SalesforceClient");

            return await client.SendAsync(request);
        }
    }
}