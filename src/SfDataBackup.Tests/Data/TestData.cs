using System;
using System.IO;
using Microsoft.Extensions.Options;
using Moq;

namespace SfDataBackup.Tests.Data
{
    public static class TestData
    {
        public const string ExtractLink = "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_1.ZIP&id=0924J000000YpZK";

        public readonly static SfOptions Options = new SfOptions
        {
            OrganisationUrl = new Uri("https://test.my.salesforce.com"),
            OrganisationId = "00D4J000000CuzU",
            OrganisationUser = "my.user@test.com",
            AppClientId = "my.client.id",
            AppCertPath = "private-key.pem",
            ExportService = new SfExportServiceOptions
            {
                Page = "/dummy/export/service/path",
                Regex = "<a\\s+href=\"(?'relurl'\\/servlet\\/servlet\\.OrgExport\\?.+?)\""
            }
        };

        public readonly static IOptionsSnapshot<SfOptions> OptionsProvider;

        public readonly static string ExportSingleExportAvailablePage;

        public readonly static string ExportNoneAvailablePage;

        public readonly static byte[] Export;

        static TestData()
        {
            var optionsProviderMock = new Mock<IOptionsSnapshot<SfOptions>>();
            optionsProviderMock.SetupGet(x => x.Value)
                                .Returns(Options);

            OptionsProvider = optionsProviderMock.Object;

            ExportSingleExportAvailablePage = File.ReadAllText("Data/ExportSingleExportAvailablePage.html");
            ExportNoneAvailablePage = File.ReadAllText("Data/ExportNoneAvailablePage.html");
            Export = File.ReadAllBytes("Data/Export.zip");
        }
    }
}