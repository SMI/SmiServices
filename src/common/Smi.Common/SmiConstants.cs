using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smi.Common
{
    /// <summary>
    /// Contains static const definitions of common concepts in Smi e.g. the default name of dicom path column in schemas
    /// </summary>
    public static class SmiConstants
    {
        public const string DefaultImagePathColumnName = "RelativeFileArchiveURI";
        public const string DefaultStudyIdColumnName = "StudyInstanceUID";
        public const string DefaultSeriesIdColumnName = "SeriesInstanceUID";
        public const string DefaultInstanceIdColumnName = "SOPInstanceUID";
    }
}
