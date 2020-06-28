using System.Collections.Generic;

namespace SfDataBackup.Consolidators
{
    public interface IZipFileConsolidator
    {
        void Consolidate(IList<string> zipFilePaths, string outputZipFilePath);
    }
}