using System.Threading.Tasks;

namespace SfDataBackup.Extractors
{
    public interface ISfExportLinkExtractor
    {
        Task<SfExportLinkExtractorResult> ExtractAsync();
    }
}