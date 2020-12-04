using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Abstractions;
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
using NLog;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using ImageMagick; // dotnet add package Magick.NET-Q16-AnyCPU

namespace Microservices.IsIdentifiable.Runners
{
    public class DicomFileRunner : IsIdentifiableAbstractRunner
    {
        private readonly IsIdentifiableDicomFileOptions _opts;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly DicomFileFailureFactory factory = new DicomFileFailureFactory();

        private readonly TesseractEngine _tesseractEngine;
        private readonly PixelTextFailureReport _tesseractReport;

        private DateTime? _zeroDate = null;

        public const string EngData = "https://github.com/tesseract-ocr/tessdata/blob/master/eng.traineddata";

        private readonly IFileSystem _fileSystem = new FileSystem();

        private readonly uint _ignoreTextLessThan;

        public DicomFileRunner(IsIdentifiableDicomFileOptions opts) : base(opts)
        {
            _opts = opts;
            _ignoreTextLessThan = opts.IgnoreTextLessThan;

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
            if (!string.IsNullOrWhiteSpace(_opts.TessDirectory))
            {
                var dir = new DirectoryInfo(_opts.TessDirectory);

                if (!dir.Exists)
                    throw new DirectoryNotFoundException("Could not find TESS directory '" + _opts.TessDirectory + "'");

                //to work with Tesseract eng.traineddata has to be in a folder called tessdata
                if (!dir.Name.Equals("tessdata"))
                    dir = dir.CreateSubdirectory("tessdata");

                var languageFile = new FileInfo(Path.Combine(dir.FullName, "eng.traineddata"));

                if (!languageFile.Exists)
                    throw new FileNotFoundException($"Could not find tesseract models file ('{languageFile.FullName}')",languageFile.FullName);

                _tesseractEngine = new TesseractEngine(dir.FullName, "eng", EngineMode.Default);
                _tesseractEngine.DefaultPageSegMode = PageSegMode.Auto;

                _tesseractReport = new PixelTextFailureReport(_opts.GetTargetName());

                Reports.Add(_tesseractReport);
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
                ValidateDicomFile(_fileSystem.FileInfo.FromFileName(file));

            //now directories
            foreach (var directory in Directory.GetDirectories(root))
                ProcessDirectory(directory);
        }


        public void ValidateDicomFile(IFileInfo fi)
        {
            _logger.Debug("Opening File: " + fi.Name);

            if (!_opts.RequirePreamble || DicomFile.HasValidHeader(fi.FullName))
            {
                var dicomFile = DicomFile.Open(fi.FullName);
                var dataSet = dicomFile.Dataset;

                if (_tesseractEngine != null)
                    ValidateDicomPixelData(fi, dicomFile, dataSet);

                foreach (var dicomItem in dataSet)
                    ValidateDicomItem(fi, dicomFile, dataSet, dicomItem);
            }
            else
            {
                _logger.Info("File does not contain valid preamble and header: " + fi.FullName);
                throw new ApplicationException("File does not contain valid preamble and header: " + fi.FullName);
            }

            DoneRows(1);
        }

        private void ValidateDicomItem(IFileInfo fi, DicomFile dicomFile, DicomDataset dataset, DicomItem dicomItem)
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
                Object value;
                try
                {
                    value = DicomTypeTranslaterReader.GetCSharpValue(dataset, dicomItem);
                }
                catch (System.FormatException)
                {
                    // TODO(rkm 2020-04-14) Fix this - we shouldn't just validate the "unknown value" string...
                    value = "Unknown value for " + dicomItem;
                }
                // Sometimes throws "Input string was not in a correct format"
                //var value = DicomTypeTranslaterReader.GetCSharpValue(dataset, dicomItem);

                if (value is string)
                    Validate(fi, dicomFile, dicomItem, value as string);

                if (value is IEnumerable<string>)
                    foreach (var s in (IEnumerable<string>)value)
                        Validate(fi, dicomFile, dicomItem, s);

                if (value is DateTime && _opts.NoDateFields && _zeroDate != (DateTime)value)
                    AddToReports(factory.Create(fi, dicomFile, value.ToString(), dicomItem.Tag.DictionaryEntry.Keyword, new[] { new FailurePart(value.ToString(), FailureClassification.Date, 0) }));

            }
        }

        private void Validate(IFileInfo fi, DicomFile dicomFile, DicomItem dicomItem, string fieldValue)
        {
            // Keyword might be "Unknown" in which case we would rather use "(xxxx,yyyy)"
            string tagName = dicomItem.Tag.DictionaryEntry.Keyword;
            //if (tagName == "Unknown" || tagName == "PrivateCreator") tagName = dicomItem.Tag.ToString(); // do this if you want PrivateCreator tags to have the numeric value preserved
            if (tagName == "Unknown") tagName = dicomItem.Tag.ToString();

            // Some strings contain null characters?!  Remove them all.
            // XXX hopefully this won't break any special character encoding (eg. UTF)
            fieldValue = fieldValue.Replace("\0", "");

            List<FailurePart> parts = Validate(tagName, fieldValue).ToList();

            if (parts.Any())
                AddToReports(factory.Create(fi, dicomFile, fieldValue, tagName, parts));
        }

        void ValidateDicomPixelData(IFileInfo fi, DicomFile dicomFile, DicomDataset ds)
        {
            string modality = GetTagOrUnknown(ds, DicomTag.Modality);
            string[] imageType = GetImageType(ds);
            string studyID = GetTagOrUnknown(ds, DicomTag.StudyInstanceUID);
            string seriesID = GetTagOrUnknown(ds, DicomTag.SeriesInstanceUID);
            string sopID = GetTagOrUnknown(ds, DicomTag.SOPInstanceUID);

            // Don't go looking for images in structured reports
            if (modality == "SR") return;

            try
            {
                DicomImage dicomImage = new DicomImage(ds);

                using (Bitmap oldBmp = dicomImage.RenderImage().AsClonedBitmap())
                {
                    using (Bitmap newBmp = new Bitmap(oldBmp)) // Strangle this line is neede for the subsequent Clone call to work
                    {
                        using (Bitmap targetBmp = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format32bppArgb))
                        {
                            Process(targetBmp, oldBmp.PixelFormat, targetBmp.PixelFormat, fi, dicomFile, sopID, studyID, seriesID, modality, imageType);

                            // Need to threshold and possibly negate the image for best results
                            // Magick.NET won't read from Bitmap directly in .net core so go via MemoryStream
                            using (MemoryStream memStreamIn = new MemoryStream())
                            using (MemoryStream memStreamOut = new MemoryStream())
                            using (MagickImage mi = new MagickImage())
                            {
                                targetBmp.Save(memStreamIn, ImageFormat.Bmp);
                                memStreamIn.Position = 0;
                                mi.Read(memStreamIn);
                                // Threshold the image to monochrome using a window size of 25 square
                                // The size 25 was determined empirically based on real images (could be larger, less effective if smaller)
                                mi.AdaptiveThreshold(25, 25);
                                // Write out to memory, can't reuse memStreamIn here as it breaks
                                mi.Write(memStreamOut);
                                memStreamOut.Position = 0;
                                Process(memStreamOut, oldBmp.PixelFormat, targetBmp.PixelFormat, fi, dicomFile, sopID, studyID, seriesID, modality, imageType);
                                // Tesseract only works with black text on white background so run again negated
                                mi.Negate();
                                memStreamOut.Position = 0;
                                mi.Write(memStreamOut);
                                memStreamOut.Position = 0;
                                Process(memStreamOut, oldBmp.PixelFormat, targetBmp.PixelFormat, fi, dicomFile, sopID, studyID, seriesID, modality, imageType);
                            }

                            //if user wants to rotate the image 90, 180 and 270 degrees
                            // XXX this is done after negation, maybe needs to be done with and without negation?
                            if (_opts.Rotate)
                                for (int i = 0; i < 3; i++)
                                {
                                    //rotate image 90 degrees and run OCR again
                                    targetBmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                    Process(targetBmp, oldBmp.PixelFormat, targetBmp.PixelFormat, fi, dicomFile, sopID, studyID, seriesID, modality, imageType, (i + 1) * 90);
                                }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                // An internal error should cause IsIdentifiable to exit
                _logger.Info(e, "Could not run Tesseract on '" + fi.FullName + "'");
                throw new ApplicationException ("Could not run Tesseract on '" + fi.FullName + "'", e);

                // OR add a message to the report saying we failed to run OCR
                //string problemField = "PixelData";
                //string text = "Error running OCR on pixel data: "+e;
                //var f = factory.Create(fi, dicomFile, text, problemField, new[] { new FailurePart(text, FailureClassification.PixelText) });
                //AddToReports(f);
                // XXX do we need this?
                //_tesseractReport.FoundPixelData(fi, sopID, pixelFormat, processedPixelFormat, studyID, seriesID, modality, imageType, meanConfidence, text.Length, text, rotationIfAny);
            }
        }

        private void Process(Bitmap targetBmp, PixelFormat pixelFormat, PixelFormat processedPixelFormat, IFileInfo fi, DicomFile dicomFile, string sopID, string studyID, string seriesID, string modality, string[] imageType, int rotationIfAny = 0)
        {
            using (var ms = new MemoryStream())
            {
                targetBmp.Save(ms,ImageFormat.Bmp);
                Process(ms, pixelFormat, processedPixelFormat, fi, dicomFile, sopID, studyID, seriesID, modality, imageType, rotationIfAny);
            }
        }

        private void Process(MemoryStream ms, PixelFormat pixelFormat, PixelFormat processedPixelFormat, IFileInfo fi, DicomFile dicomFile, string sopID, string studyID, string seriesID, string modality, string[] imageType, int rotationIfAny = 0)
        {
            float meanConfidence;
            string text;

            var bytes = ms.ToArray();

            // targetBmp is now in the desired format.
            using (var page = _tesseractEngine.Process(Pix.LoadFromMemory(bytes)))
            {
                text = page.GetText();
                text = Regex.Replace(text, @"\t|\n|\r", " ");   // XXX abrooks surely more useful to have a space?
                text = text.Trim();
                meanConfidence = page.GetMeanConfidence();
            }
            
            
            //if we find some text
            if (!string.IsNullOrWhiteSpace(text))
            {
                string problemField = rotationIfAny != 0 ? "PixelData" + rotationIfAny : "PixelData";
                
                if(text.Length < _ignoreTextLessThan)
                    _logger.Debug($"Ignoring pixel data discovery in {fi.Name} of length {text.Length} because it is below the threshold {_ignoreTextLessThan}");
                else
                {
                    var f = factory.Create(fi, dicomFile, text, problemField, new[] { new FailurePart(text, FailureClassification.PixelText) });

                    AddToReports(f);

                    _tesseractReport.FoundPixelData(fi, sopID, pixelFormat, processedPixelFormat, studyID, seriesID, modality, imageType, meanConfidence, text.Length, text, rotationIfAny);
                }                
            }
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
    }
}
