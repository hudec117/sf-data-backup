using System;

namespace SfDataBackup
{
    public class SfConfig
    {
        public Uri OrganisationUrl { get; set; }

        public string OrganisationId { get; set; }

        public string AccessToken { get; set; }

        public SfConfig()
        {
        }

        public SfConfig(SfConfig otherConfig)
        {
            OrganisationUrl = otherConfig.OrganisationUrl;
            OrganisationId = otherConfig.OrganisationId;
            AccessToken = otherConfig.AccessToken;
        }
    }
}