using System.Collections.Generic;

namespace SfDataBackup.Consolidators
{
    public interface ISfExportConsolidator
    {
        SfResult Consolidate(IList<string> exportPaths, string consolidatedExportPath);
    }
}