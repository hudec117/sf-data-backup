using System.Collections.Generic;

namespace SfDataBackup.Consolidators
{
    public interface ISfExportConsolidator
    {
        void Consolidate(IList<string> exportPaths);
    }
}