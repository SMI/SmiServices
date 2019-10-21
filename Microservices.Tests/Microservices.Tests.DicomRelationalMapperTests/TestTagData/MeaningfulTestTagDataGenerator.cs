using System;
using System.Collections.Generic;
using System.Linq;
using BadMedicine;
using Dicom;
using DicomTypeTranslation;
using DicomTypeTranslation.TableCreation;
using FAnsi.Discovery;
using FAnsi.Implementations.MicrosoftSQL;
using Rdmp.Dicom.TagPromotionSchema;

namespace Microservices.Tests.RDMPTests.TestTagData
{
    /// <summary>
    /// Creates more representative data given the set of tags in the specified Template
    /// </summary>
    class MeaningfulTestTagDataGenerator
    {
        private Dictionary<DatabaseColumnRequest, DicomDictionaryEntry> _columns = new Dictionary<DatabaseColumnRequest, DicomDictionaryEntry>();
        
        Random r;

        TestTagDataGenerator _basicTags = new TestTagDataGenerator();

        public MeaningfulTestTagDataGenerator(ImageTableTemplateCollection collection, Random random)
        {
            r = random;
            var itc = new ImagingTableCreation(new MicrosoftQuerySyntaxHelper());

            foreach (var col in collection.Tables.SelectMany(c => c.Columns).Select(c => c.ColumnName).Distinct())
            {
                var tag = TagColumnAdder.GetTag(col);

                if(tag != null)
                    _columns.Add(itc.GetColumnDefinition(col),tag);
            }

        }

        public DicomDataset[] GenerateDatasets(int numberInSeries)
        {
            Person p = new Person(r);

            var studyid = Guid.NewGuid().ToString();
            var seriesid = Guid.NewGuid().ToString();
            var studyDate = p.GetRandomDateDuringLifetime(r);
            var ae = "abc" + r.Next(100000);

            List<DicomDataset> toReturn = new List<DicomDataset>();
            
            for (int i = 0; i < numberInSeries; i++)
            {
                DicomDataset ds = new DicomDataset();
                
                foreach (var kvp in _columns)
                {
                    object val = GetValueForColumn(studyid, seriesid,studyDate, numberInSeries, p, kvp.Key, kvp.Value, ae);
                    
                    if(val != null)
                        DicomTypeTranslaterWriter.SetDicomTag(ds, kvp.Value, val);
                }
                toReturn.Add(ds);
            }
            
            return toReturn.ToArray();
        }
        
        private object GetValueForColumn(object studyid, object seriesid, DateTime studyDate, int numberInSeries, Person p, DatabaseColumnRequest c, DicomDictionaryEntry entry, object ae)
        {
            switch (c.ColumnName)
            {
                //Study
                case "PatientID":return p.CHI;
                case "StudyInstanceUID": return studyid;
                case "SeriesInstanceUID": return seriesid;
                case "StudyTime": return null;
                case "ModalitiesInStudy": return "CT";
                case "StudyDescription": return "Imaginary made up study";
                case "AccessionNumber": return null;
                case "PatientSex": return p.Gender.ToString();
                case "PatientAge": return ((int)(DateTime.Now.Subtract(p.DateOfBirth).TotalDays/365)) + "Y";
                case "StudyDate": return studyDate;
                case "NumberOfStudyRelatedInstances": return numberInSeries;

                //Series
                case "Modality": return "CT";
                case "SourceApplicationEntityTitle": return ae;
                case "InstitutionName": return p.Address.Line1;
                case "ProcedureCodeSequence": return null;
                case "ProtocolName": return null;
                case "PerformedProcedureStepID": return null;
                case "PerformedProcedureStepDescription": return null;
                case "SeriesDescription": return null;
                case "BodyPartExamined": return null;
                case "DeviceSerialNumber": return null;
                case "NumberOfSeriesRelatedInstances": return numberInSeries;
                case "SeriesNumber": return 1;

                //Image
                case "SOPInstanceUID": return Guid.NewGuid().ToString();
                case "SeriesDate": return p.GetRandomDateDuringLifetime(r);
                case "SeriesTime": return null;
                case "BurnedInAnnotation": return r.Next(2)==1?"YES":"NO";
                case "SliceLocation": return null;
                case "SliceThickness": return null;
                case "SpacingBetweenSlices": return null;
                case "SpiralPitchFactor": return null;
                case "KVP": return null;
                case "ExposureTime": return null;
                case "Exposure": return null;
                case "ImageType": return null;
                case "ManufacturerModelName": return null;
                case "Manufacturer": return null;
                case "XRayTubeCurrent": return null;
                case "PhotometricInterpretation": return null;
                case "ContrastBolusRoute": return null;
                case "ContrastBolusAgent": return null;
                case "AcquisitionNumber": return null;
                case "AcquisitionDate": return null;
                case "AcquisitionTime": return null;
                case "ImagePositionPatient": return null;
                case "PixelSpacing": return null;
                case "FieldOfViewDimensions": return null;
                case "FieldOfViewDimensionsInFloat": return null;
                case "DerivationDescription": return null;
                case "TransferSyntaxUID": return null;
                case "LossyImageCompression": return null;
                case "LossyImageCompressionMethod": return null;
                case "LossyImageCompressionRatio": return null;
                case "LossyImageCompressionRetired": return null;
                case "ScanOptions"         :return null;              

                default:
                    return _basicTags.GetRandomValue(entry,r);
            }
        }
    }
}
