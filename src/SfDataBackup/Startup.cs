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

            builder.Services.AddScoped<ISfExportLinkExtractor>(serviceProvider =>
            {
                var config = new SfExportLinkExtractorConfig
                {
                    ServerUrl = "",
                    AccessToken = "",
                    OrganisationId = ""
                };

                return new SfExportLinkExtractor(config);
            });
        }
    }
}