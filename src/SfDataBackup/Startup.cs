using System.IO.Abstractions;
using System.Net.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SfDataBackup.Consolidators;
using SfDataBackup.Downloaders;
using SfDataBackup.Extractors;
using SfDataBackup.Services;

[assembly: FunctionsStartup(typeof(SfDataBackup.Startup))]
namespace SfDataBackup
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
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

            builder.Services.AddOptions<SfOptions>()
                            .Configure<IConfiguration>((options, configuration) =>
                            {
                                configuration.GetSection("Salesforce").Bind(options);
                            });

            // Register file system
            builder.Services.AddScoped<IFileSystem, FileSystem>();

            // Register the JWT authentication service
            builder.Services.AddScoped<ISfService, SfService>();

            // Register link extractor
            builder.Services.AddScoped<ISfExportLinkExtractor, SfExportLinkExtractor>();

            // Register export downloader
            builder.Services.AddScoped<ISfExportDownloader, SfExportDownloader>();

            // Register export consolidator
            builder.Services.AddScoped<ISfExportConsolidator, SfExportConsolidator>();
        }
    }
}