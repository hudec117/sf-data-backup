using System;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SfDataBackup.Consolidators;
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
                OrganisationUser = Environment.GetEnvironmentVariable("SALESFORCE_ORG_USER"),
                AppClientId = Environment.GetEnvironmentVariable("SALESFORCE_APP_CLIENT_ID"),
                AppCertPath = Environment.GetEnvironmentVariable("SALESFORCE_APP_CERT")
            };

            // Register logging services
            builder.Services.AddLogging();

            // Configure a HTTP client with authentication cookies.
            builder.Services.AddHttpClient("DefaultClient")
                            .ConfigurePrimaryHttpMessageHandler(() =>
                            {
                                return new SocketsHttpHandler
                                {
                                    UseCookies = false
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

            // Register file system
            builder.Services.AddScoped<IFileSystem, FileSystem>();

            // Register link extractor
            builder.Services.AddScoped<ISfExportLinkExtractor, SfExportLinkExtractor>();

            // Register export downloader
            builder.Services.AddScoped<ISfExportDownloader, SfSerialExportDownloader>();

            // Register export consolidator
            builder.Services.AddScoped<ISfExportConsolidator, SfExportConsolidator>();
        }
    }
}