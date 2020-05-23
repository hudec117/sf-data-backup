using System;
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
            builder.Services.AddLogging();

            builder.Services.AddHttpClient();

            var organisationUrl = Environment.GetEnvironmentVariable("SALESFORCE_ORG_URL");

            var config = new SfConfig
            {
                OrganisationUrl = new Uri(organisationUrl),
                OrganisationId = Environment.GetEnvironmentVariable("SALESFORCE_ORG_ID"),
                AccessToken = "dummy.jwt.token"
            };

            builder.Services.AddSingleton<SfConfig>(config);

            builder.Services.AddSingleton<SfExportLinkExtractorConfig>(serviceProvider =>
            {
                return new SfExportLinkExtractorConfig(config)
                {
                    ExportServicePath = Environment.GetEnvironmentVariable("EXPORT_SERVICE_PATH"),
                    ExportServiceRegex = Environment.GetEnvironmentVariable("EXPORT_SERVICE_REGEX")
                };
            });

            builder.Services.AddScoped<ISfExportLinkExtractor, SfExportLinkExtractor>();
        }
    }
}