using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Options;
using Moq;

namespace SfDataBackup.Tests.Data
{
    public static class TestData
    {
        public static readonly IList<string> ExportDownloadLinks = new List<string>
        {
            "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_1.ZIP&id=0924J000000YpZK",
            "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_2.ZIP&id=0924J000000YpZK",
            "/servlet/servlet.OrgExport?fileName=WE_00D4J000000CuzUUAS_3.ZIP&id=0924J000000YpZK"
        };

        public static readonly SfOptions Options = new SfOptions
        {
            OrganisationUrl = new Uri("https://test.my.salesforce.com"),
            Username = "my.user@test.com",
            Password = "dummypassword",
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