using System.Collections.Generic;

namespace SfDataBackup.Consolidators
{
    public interface IZipFileConsolidator
    {
        string Consolidate(IList<string> zipFilePaths);
    }
}