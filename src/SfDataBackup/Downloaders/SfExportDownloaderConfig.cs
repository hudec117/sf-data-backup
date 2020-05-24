namespace SfDataBackup.Downloaders
{
    public class SfExportDownloaderConfig : SfConfig
    {
        public string DownloadPath { get; set; }

        public SfExportDownloaderConfig(SfConfig config) : base(config)
        {
        }
    }
}