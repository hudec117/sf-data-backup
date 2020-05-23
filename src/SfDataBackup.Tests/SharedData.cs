using System;
using System.IO;

namespace SfDataBackup.Tests
{
    public static class SharedData
    {
        public static SfConfig Config => new SfConfig
        {
            OrganisationUrl = new Uri("https://abcd123.my.salesforce.com"),
            AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            OrganisationId = "00D4J000000CuzU"
        };

        public readonly static string ExportPageSingleExportAvailable;

        static SharedData()
        {
            ExportPageSingleExportAvailable = File.ReadAllText("Data/ExportPageSingleExportAvailable.html");
        }
    }
}