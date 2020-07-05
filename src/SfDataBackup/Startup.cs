using System;
using System.IO.Abstractions;
using System.Net.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SfDataBackup.Abstractions;
using SfDataBackup.Consolidators;
using SfDataBackup.Services;
using SfDataBackup.Services.Auth;

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
            builder.Services.AddHttpClient("SalesforceClient")
                            .ConfigureHttpClient(client =>
                            {
                                client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("Salesforce:OrganisationUrl"));
                            })
                            .ConfigurePrimaryHttpMessageHandler(() =>
                            {
                                // Need to set UseCookies to false so that the handler
                                // does not use it's own cookie container and prevent
                                // us from setting cookies dynamically.
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

            // Register ZIP file abstraction
            builder.Services.AddScoped<IZipFile, ZipFile>();

            // Register ZIP file consolidator
            builder.Services.AddScoped<IZipFileConsolidator, ZipFileConsolidator>();

            // Register the JWT authentication service
            builder.Services.AddScoped<ISfAuthService, SfAuthService>();

            // Register Salesforce Service
            builder.Services.AddScoped<ISfService, SfService>();
        }
    }
}