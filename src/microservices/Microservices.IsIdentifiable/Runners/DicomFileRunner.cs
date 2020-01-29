using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Dicom;
using Dicom.Imaging;
using DicomTypeTranslation;
using Microservices.IsIdentifiable.Failures;
using Microservices.IsIdentifiable.Options;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Reporting.Reports;
using Microservices.IsIdentifiable.Rules;
using NLog;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Microservices.IsIdentifiable.Runners
{
    internal class DicomFileRunner : IsIdentifiableAbstractRunner
    {
        private readonly IsIdentifiableDicomFileOptions _opts;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly DicomFileFailureFactory factory = new DicomFileFailureFactory();

        private readonly PixelTextFailureReport _tesseractReport;

        private DateTime? _zeroDate = null;
        private SocketRule _ocrHost;

        public const string EngData = "https://github.com/tesseract-ocr/tessdata/blob/master/eng.traineddata";

        public DicomFileRunner(IsIdentifiableDicomFileOptions opts) : base(opts)
        {
            _opts = opts;

            //if using Efferent.Native DICOM codecs
            // (see https://github.com/Efferent-Health/Dicom-native)
            //Dicom.Imaging.Codec.TranscoderManager.SetImplementation(new Efferent.Native.Codec.NativeTranscoderManager());

            //OR if using fo-dicom.Native DICOM codecs
            // (see https://github.com/fo-dicom/fo-dicom/issues/631)
            ImageManager.SetImplementation(new WinFormsImageManager());

            //if there is a value we are treating as a zero date
            if (!string.IsNullOrWhiteSpace(_opts.ZeroDate))
                _zeroDate = DateTime.Parse(_opts.ZeroDate);

            //if the user wants to run text detection
            if (!string.IsNullOrWhiteSpace(_opts.OCRHost))
            {
                _tesseractReport = new PixelTextFailureReport(_opts.GetTargetName());
                Reports.Add(_tesseractReport);
                _ocrHost = new SocketRule()
                {
                    Host = _opts.OCRHost.Split(':')[0],
                    Port = int.Parse(_opts.OCRHost.Split(':')[1]),
                };
            }
        }

        public override int Run()
        {
            _logger.Info("Recursing from Directory: " + _opts.Directory);

            if (!Directory.Exists(_opts.Directory))
            {
                _logger.Info("Cannot Find directory: " + _opts.Directory);
                throw new ArgumentException("Cannot Find directory: " + _opts.Directory);
            }

            ProcessDirectory(_opts.Directory);

            CloseReports();

            return 0;
        }

        private void ProcessDirectory(string root)
        {
            //deal with files first
            foreach (var file in Directory.GetFiles(root, _opts.Pattern))
                ValidateDicomFile(new FileInfo(file));

            //now directories
            foreach (var directory in Directory.GetDirectories(root))
                ProcessDirectory(directory);
        }


        public void ValidateDicomFile(FileInfo fi)
        {
            _logger.Debug("Opening File: " + fi.Name);

            if (!_opts.RequirePreamble || DicomFile.HasValidHeader(fi.FullName))
            {
                var dicomFile = DicomFile.Open(fi.FullName);
                var dataSet = dicomFile.Dataset;

                if (_ocrHost != null)
                    if (_ocrHost.Apply("path", fi.FullName, out IEnumerable<FailurePart> parts) == RuleAction.Report)
                    {
                        string modality = GetTagOrUnknown(dataSet, DicomTag.Modality);
                        string[] imageType = GetImageType(dataSet);
                        string studyID = GetTagOrUnknown(dataSet, DicomTag.StudyInstanceUID);
                        string seriesID = GetTagOrUnknown(dataSet, DicomTag.SeriesInstanceUID);
                        string sopID = GetTagOrUnknown(dataSet, DicomTag.SOPInstanceUID);

                        // Don't go looking for images in structured reports
                        if (modality == "SR") return;

                        foreach (var part in parts)
                        {
                            if(part.Classification != FailureClassification.PixelText)
                                throw new Exception($"OCR service returned {part.Classification} (expected PixelText)");
                        
                            _tesseractReport.FoundPixelData(fi,sopID,studyID,seriesID,modality,imageType,0,0,part.Word,0);
                        }

                        

                    }

                foreach (var dicomItem in dataSet)
                    ValidateDicomItem(fi, dicomFile, dataSet, dicomItem);
            }
            else
                _logger.Info("File does not contain valid preamble and header: " + fi.FullName);

            DoneRows(1);
        }

        private void ValidateDicomItem(FileInfo fi, DicomFile dicomFile, DicomDataset dataset, DicomItem dicomItem)
        {
            //if it is a sequence get the Sequences dataset and then start processing that
            if (dicomItem.ValueRepresentation.Code == "SQ")
            {
                var sequenceItemDataSets = dataset.GetSequence(dicomItem.Tag);
                foreach (var sequenceItemDataSet in sequenceItemDataSets)
                    foreach (var sequenceItem in sequenceItemDataSet)
                        ValidateDicomItem(fi, dicomFile, sequenceItemDataSet, sequenceItem);
            }
            else
            {
                var value = DicomTypeTranslaterReader.GetCSharpValue(dataset, dicomItem);

                if (value is string)
                    Validate(fi, dicomFile, dicomItem, value as string);

                if (value is IEnumerable<string>)
                    foreach (var s in (IEnumerable<string>)value)
                        Validate(fi, dicomFile, dicomItem, s);

                if (value is DateTime && _opts.NoDateFields && _zeroDate != (DateTime)value)
                    AddToReports(factory.Create(fi, dicomFile, value.ToString(), dicomItem.Tag.DictionaryEntry.Keyword, new[] { new FailurePart(value.ToString(), FailureClassification.Date, 0) }));

            }
        }

        private void Validate(FileInfo fi, DicomFile dicomFile, DicomItem dicomItem, string fieldValue)
        {
            List<FailurePart> parts = Validate(dicomItem.Tag.DictionaryEntry.Keyword, fieldValue).ToList();

            if (parts.Any())
                AddToReports(factory.Create(fi, dicomFile, fieldValue, dicomItem.Tag.ToString(), parts));
        }

        /// <summary>
        /// Returns a 3 element array of the Dicom ImageType tag.  If there are less than 3 elements in the dataset it returns nulls.  If
        /// there are more than 3 elements it sets the final element to all remaining elements joined with backslashes 
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        string[] GetImageType(DicomDataset ds)
        {
            string[] result = new string[3];

            if (ds.Contains(DicomTag.ImageType))
            {
                string[] values = ds.GetValues<string>(DicomTag.ImageType);
                if (values.Length > 0)
                {
                    result[0] = values[0];
                }
                if (values.Length > 1)
                {
                    result[1] = values[1];
                }
                if (values.Length > 2)
                {
                    result[2] = "";
                    for (int i = 2; i < values.Length; ++i)
                    {
                        result[2] = result[2] + "\\" + values[i];
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the value of the tag or null if it is not contained.  Tag must be a string element
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        string GetTagOrUnknown(DicomDataset ds, DicomTag dt)
        {
            if (ds.Contains(dt))
                return ds.GetValue<string>(dt, 0);

            return null;
        }

        public override void Dispose()
        {
            base.Dispose();

            _ocrHost?.Dispose();
        }
    }
}
