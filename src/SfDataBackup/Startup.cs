using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SfDataBackup.Extractors;

namespace SfDataBackup
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();

            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<SfExportLinkExtractorConfig>(serviceProvider =>
            {
                return new SfExportLinkExtractorConfig
                {
                    ServerUrl = new Uri("https://ahudecdevopstools-dev-ed.my.salesforce.com"),
                    AccessToken = "",
                    OrganisationId = "00D4J000000CuzU"
                };
            });

            builder.Services.AddSingleton<ISfExportLinkExtractor, SfExportLinkExtractor>();
        }
    }
}