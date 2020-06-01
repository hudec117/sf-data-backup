using System.Collections.Generic;

namespace SfDataBackup.Downloaders
{
    public interface ISfExportConsolidator
    {
        void Consolidate(IList<string> exportPaths);
    }
}