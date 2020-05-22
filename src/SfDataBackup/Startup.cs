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

            builder.Services.AddSingleton(serviceProvider =>
            {
                return new SfExportLinkExtractorConfig
                {
                    ServerUrl = "",
                    AccessToken = "",
                    OrganisationId = ""
                };
            });

            builder.Services.AddTransient<ISfExportLinkExtractor, SfExportLinkExtractor>();
        }
    }
}