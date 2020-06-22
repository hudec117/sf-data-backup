using System;
using System.IO;

namespace SfDataBackup.Tests.Data
{
    public static class TestData
    {
        public static SfConfig Config => new SfConfig
        {
            OrganisationUrl = new Uri("https://abc123.my.salesforce.com"),
            OrganisationId = "00D4J000000CuzU",
            OrganisationUser = "my.user@abc123.com"
        };

        public readonly static string ExportSingleExportAvailablePage;

        public readonly static string ExportNoneAvailablePage;

        public readonly static byte[] Export;

        static TestData()
        {
            ExportSingleExportAvailablePage = File.ReadAllText("Data/ExportSingleExportAvailablePage.html");
            ExportNoneAvailablePage = File.ReadAllText("Data/ExportNoneAvailablePage.html");
            Export = File.ReadAllBytes("Data/Export.zip");
        }
    }
}