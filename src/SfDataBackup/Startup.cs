using System;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
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

            builder.Services.AddLogging();

            // Configure an HTTP client with authentication cookies.
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

            // Register the generic config and export service config.
            builder.Services.AddSingleton<SfConfig>(config);

            builder.Services.AddSingleton<SfExportLinkExtractorConfig>(serviceProvider =>
            {
                return new SfExportLinkExtractorConfig(config)
                {
                    ExportServicePath = Environment.GetEnvironmentVariable("EXPORT_SERVICE_PATH"),
                    ExportServiceRegex = Environment.GetEnvironmentVariable("EXPORT_SERVICE_REGEX")
                };
            });

            // Register link extractor
            builder.Services.AddScoped<ISfExportLinkExtractor, SfExportLinkExtractor>();
        }
    }
}