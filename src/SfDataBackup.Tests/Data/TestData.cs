using System;
using System.IO;

namespace SfDataBackup.Tests.Data
{
    public static class TestData
    {
        public static SfConfig Config => new SfConfig
        {
            OrganisationUrl = new Uri("https://abc123.my.salesforce.com"),
            AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            OrganisationId = "00D4J000000CuzU"
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