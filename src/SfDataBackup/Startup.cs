using System;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SfDataBackup.Downloaders;
using SfDataBackup.Extractors;

[assembly: FunctionsStartup(typeof(SfDataBackup.Startup))]
namespace SfDataBackup
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Load config
            var rawOrganisationUrl = Environment.GetEnvironmentVariable("SALESFORCE_ORG_URL");

            var config = new SfConfig
            {
                OrganisationUrl = new Uri(rawOrganisationUrl),
                OrganisationId = Environment.GetEnvironmentVariable("SALESFORCE_ORG_ID"),
                AccessToken = "dummy.jwt.token"
            };

            // Register logging services
            builder.Services.AddLogging();

            // Configure a HTTP client with authentication cookies.
            builder.Services.AddHttpClient("SalesforceClient", client =>
            {
                client.DefaultRequestVersion = HttpVersion.Version20;
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var cookieContainer = new CookieContainer();
                var oidCookie = new Cookie("oid", config.OrganisationId);
                var sidCookie = new Cookie("sid", config.AccessToken);
                cookieContainer.Add(config.OrganisationUrl, oidCookie);
                cookieContainer.Add(config.OrganisationUrl, sidCookie);

                return new SocketsHttpHandler
                {
                    CookieContainer = cookieContainer
                };
            });

            // Register configs.
            builder.Services.AddSingleton<SfConfig>(config);

            builder.Services.AddSingleton<SfExportLinkExtractorConfig>(serviceProvider =>
            {
                return new SfExportLinkExtractorConfig(config)
                {
                    ExportServicePath = Environment.GetEnvironmentVariable("EXPORT_SERVICE_PATH"),
                    ExportServiceRegex = Environment.GetEnvironmentVariable("EXPORT_SERVICE_REGEX")
                };
            });

            builder.Services.AddSingleton<SfExportDownloaderConfig>(serviceProvider =>
            {
                return new SfExportDownloaderConfig(config)
                {
                    DownloadPath = Environment.GetEnvironmentVariable("EXPORT_DOWNLOAD_PATH")
                };
            });

            // Register file system
            builder.Services.AddScoped<IFileSystem, FileSystem>();

            // Register link extractor
            builder.Services.AddScoped<ISfExportLinkExtractor, SfExportLinkExtractor>();

            // Register export downloader
            builder.Services.AddScoped<ISfExportDownloader, SfExportDownloader>();
        }
    }
}