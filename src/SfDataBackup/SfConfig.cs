using System;

namespace SfDataBackup
{
    public class SfConfig
    {
        public Uri OrganisationUrl { get; set; }

        public string OrganisationId { get; set; }

        public string OrganisationUser { get; set; }

        public string AppClientId { get; set; }

        public string AppCertPath { get; set; }

        public SfConfig()
        {
        }

        public SfConfig(SfConfig otherConfig)
        {
            OrganisationUrl = otherConfig.OrganisationUrl;
            OrganisationId = otherConfig.OrganisationId;
            OrganisationUser = otherConfig.OrganisationUser;
            AppClientId = otherConfig.AppClientId;
            AppCertPath = otherConfig.AppCertPath;
        }
    }
}