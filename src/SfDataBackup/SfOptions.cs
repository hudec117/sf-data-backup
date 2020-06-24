using System;

namespace SfDataBackup
{
    public class SfOptions
    {
        public Uri OrganisationUrl { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public SfExportServiceOptions ExportService { get; set; }
    }

    public class SfExportServiceOptions
    {
        public string Page { get; set; }

        public string Regex { get; set; }
    }
}