using System;

namespace SfDataBackup
{
    public class SfOptions
    {
        public Uri OrganisationUrl { get; set; }

        public string OrganisationId { get; set; }

        public string OrganisationUser { get; set; }

        public string AppClientId { get; set; }

        public string AppCertPath { get; set; }

        public SfExportServiceOptions ExportService { get; set; }
    }

    public class SfExportServiceOptions
    {
        public string Page { get; set; }

        public string Regex { get; set; }
    }
}