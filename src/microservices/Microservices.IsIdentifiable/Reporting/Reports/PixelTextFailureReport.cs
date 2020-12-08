using System.Data;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Abstractions;


namespace Microservices.IsIdentifiable.Reporting.Reports
{
    internal class PixelTextFailureReport : FailureReport
    {
        private readonly DataTable _dt = new DataTable();

        private readonly string[] _headerRow =
        {
            "Filename",
            "SOPInstanceUID",
            "PixelFormat",
            "ProcessedPixelFormat",
            "StudyInstanceUID",
            "SeriesInstanceUID",
            "Modality",
            "ImageType1",
            "ImageType2",
            "ImageType3",
            "MeanConfidence",
            "TextLength",
            "PixelText",
            "Rotation"
        };

        public PixelTextFailureReport(string targetName)
            : base(targetName)
        {
            foreach (string s in _headerRow)
                _dt.Columns.Add(s);
        }

        public override void Add(Failure failure)
        {

        }

        protected override void CloseReportBase()
        {
            Destinations.ForEach(d => d.WriteItems(_dt));
        }

        //TODO Replace argument list with object
        public void FoundPixelData(IFileInfo fi, string sopID, PixelFormat pixelFormat, PixelFormat processedPixelFormat, string studyID, string seriesID, string modality, string[] imageType, float meanConfidence, int textLength, string pixelText, int rotation)
        {
            DataRow dr = _dt.Rows.Add();

            if (imageType != null && imageType.Length > 0)
                dr["ImageType1"] = imageType[0];
            if (imageType != null && imageType.Length > 1)
                dr["ImageType2"] = imageType[1];
            if (imageType != null && imageType.Length > 2)
                dr["ImageType3"] = imageType[2];

            //TODO Pull these out
            dr["Filename"] = fi.FullName;
            dr["SOPInstanceUID"] = sopID;
            dr["PixelFormat"] = pixelFormat;
            dr["ProcessedPixelFormat"] = processedPixelFormat;

            dr["StudyInstanceUID"] = studyID;

            dr["SeriesInstanceUID"] = seriesID;
            dr["Modality"] = modality;

            dr["MeanConfidence"] = meanConfidence;
            dr["TextLength"] = textLength;
            dr["PixelText"] = pixelText;
            dr["Rotation"] = rotation;
        }
    }
}