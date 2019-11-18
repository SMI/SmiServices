using System.Linq;
using Dicom;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using DicomTypeTranslation;
using System.IO.Abstractions;
using System.Threading;

namespace Smi.Common.Tests
{
    public class DicomGenerator : IDisposable
    {
        private string _outputDir;
        private string _seedPath;

        private Random _random;

        private bool _deleteAfter;

        private List<string> _patientIDs;

        private int _totalGenerated;
        

        private const string DICOM_FILE_COLUMN_NAME = "DicomFile";
        private const string OUTPUT_PATH_COLUMN_NAME = "FilePath";

        public List<FileInfo> FilesCreated { get; set; }
        public HashSet<DicomTag> RandomTagsAdded = new HashSet<DicomTag>();
       

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseDir">Base dir to work from.</param>
        /// <param name="seedObject">Either a path to a single dicom file or a directory containing dicom files. Relative to baseDir.</param>
        public DicomGenerator(string baseDir, string seedObject, int randomSeed = 0, bool deleteAfter = true)
        {
            //TODO  validation
            //TODO Check small no of seed files

            var seedPath = baseDir + "/" + seedObject;

            if (!Directory.Exists(seedPath) && !File.Exists(seedPath))
                throw new ArgumentException("File or directory does not exist: " + seedPath);
            
            _outputDir = baseDir + @"/GeneratedDicom";
            Directory.CreateDirectory(_outputDir);

            _seedPath = seedPath;

            _random = (randomSeed != 0) ? new Random(randomSeed) : new Random();
            _deleteAfter = deleteAfter;

            FilesCreated = new List<FileInfo>();
        }


        public void GenerateTestSet(int nImages, int nPatients)
        {
            GenerateTestSet(nImages, nPatients, null,0,true);

        }
        public void GenerateTestSet(int nImages, int nPatients, ITagRandomiser randomiser, int numberOfRandomTagsPerFile, bool generateCorruptFile)
        {
            //TODO Input validation
            
            GeneratePatientIDs(nPatients);

            var seedFiles = new List<DicomFile>();

            if (_seedPath.EndsWith(".dcm"))
            {
                // Single dicom file
                seedFiles.Add(DicomFile.Open(_seedPath));
            }
            else
            {
                // Directory of files to read in          
                seedFiles = ReadSeedSet();
            }
            
            if (seedFiles.Count == 0)
            {
                Console.WriteLine("No seed files found");
                return;
            }

            var seedSet = GenerateSeedSet(seedFiles, nImages);

            _totalGenerated = 0;

            while (_totalGenerated < nImages)
                GenerateImages(seedSet, nImages,randomiser,numberOfRandomTagsPerFile);            

            if(generateCorruptFile)
                GenerateCorruptFile((DicomFile) seedSet.Rows[0][DICOM_FILE_COLUMN_NAME]);

            GenerateNonDicomFile();

            //TODO Check number of patients in output equal to nPatients
            return;
        }

        private List<DicomFile> ReadSeedSet()
        {
            var seedFiles = new List<DicomFile>();

            IFileSystem fileSystem = new FileSystem();

            try
            {
                var dicomFilePaths = fileSystem.Directory.EnumerateFiles(_seedPath, "*.dcm");

                foreach (var dicomFilePath in dicomFilePaths)
                {
                    try
                    {
                        seedFiles.Add(DicomFile.Open(dicomFilePath));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception when opening dicom file: " + e.Message);
                    }
                }

            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("No dicom files found in: " + _seedPath);
            }

            return seedFiles;
        }


        private void GeneratePatientIDs(int nPatients)
        {
            _patientIDs = new List<string>();

            for (var i = 0; i < nPatients; i++)
                _patientIDs.Add((100000 + i).ToString("D6"));
        }
        
        private DataTable GenerateSeedSet(List<DicomFile> seedFiles, int nImages)
        {
            DataTable seedSet = new DataTable();
            DataColumn column;

            column = new DataColumn(DICOM_FILE_COLUMN_NAME);
            column.DataType = typeof(DicomFile);
            seedSet.Columns.Add(column);

            column = new DataColumn(OUTPUT_PATH_COLUMN_NAME);
            column.DataType = typeof(System.String);
            seedSet.Columns.Add(column);

            //TODO Magic numbers!
            // Estimate number of accession & day directories we need... not sure about this
            int meanImagesPerDir = 20;
            int requiredAccDirs = 1 + nImages / meanImagesPerDir;
            int requiredDays = 1 + requiredAccDirs / 30;
            
            DataRow row;
            DicomFile dFile;
            string curPatientID;
            string outputPath;
            
            int seedFileCounter = 0;
            
            for (var day = 0; day < requiredDays; day++)
            {
                for (var acc = 0; acc < requiredAccDirs / requiredDays; acc++)
                {
                    for(var im = 0; im < seedFiles.Count; im++)
                    {
                        //Console.WriteLine("day: " + (day+1) + ", acc: "+ (acc+1));

                        dFile = seedFiles[(seedFileCounter++) % seedFiles.Count].Clone();
                        curPatientID = getNextPatientID();

                        dFile.Dataset.AddOrUpdate(DicomTag.PatientID, "Patient" + curPatientID);
                        dFile.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
                        dFile.Dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());

                        //TODO What else do we want to update?
                        //dFile.Dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, ...);
                        //dFile.Dataset.AddOrUpdate(DicomTag.SeriesNumber, ...);
                        //dFile.Dataset.AddOrUpdate(DicomTag.Modality, ...);

                        //TODO Make this into a proper YYYY/MM/DD/ACC structure
                        outputPath = String.Format("2018/01/{0:00}/{1:00}/", day + 1, acc + 1);
                        
                        row = seedSet.NewRow();
                        row[DICOM_FILE_COLUMN_NAME] = dFile;
                        row[OUTPUT_PATH_COLUMN_NAME] = outputPath;
                        
                        seedSet.Rows.Add(row);
                    }
                }
            }            
            
            return seedSet;
        }

        static int _currentPatientId = 0;
        private string getNextPatientID()
        {
            try
            {
                if (_currentPatientId > _patientIDs.Count)
                    throw new ArgumentException("Ran out of patients");

                return _patientIDs[_currentPatientId];
            }
            finally
            {
                Interlocked.Increment(ref _currentPatientId);
            }
        }


        private void GenerateImages(DataTable seedSet, int nImages, ITagRandomiser randomiser, int numberOfRandomTagsPerFile)
        {
            int imagesInSeries;
            DicomFile seedFile;

            foreach (DataRow row in seedSet.Rows)
            {
                seedFile = (DicomFile)row[DICOM_FILE_COLUMN_NAME];

                Directory.CreateDirectory(_outputDir + "/" + (string)row[OUTPUT_PATH_COLUMN_NAME]);

                //TODO Magic number!
                imagesInSeries = _random.Next(1, 20);

                if (imagesInSeries + _totalGenerated > nImages)
                    imagesInSeries = nImages - _totalGenerated;

                for (var i = 0; i < imagesInSeries; i++)
                {
                    //open it
                    var newFile = seedFile.Clone();

                    //change tags
                    newFile.Dataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
                    
                    for (int j = 0; j < numberOfRandomTagsPerFile; j++)
                    {
                        var tag = randomiser.GetRandomTag(_random);

                        //Don't generate UIds
                        if(tag.ValueRepresentations.Contains(DicomVR.UI))
                            continue;

                        var value = randomiser.GetRandomValue(tag, _random);
                        
                        if (value == null)
                            continue;

                        RandomTagsAdded.Add(tag.Tag);

                        if (!newFile.Dataset.Contains(tag.Tag))
                            DicomTypeTranslaterWriter.SetDicomTag(newFile.Dataset, tag, value);
                    }

                    //create a copy
                    string outFileName = _outputDir + "/" + (string)row[OUTPUT_PATH_COLUMN_NAME] + "image-" + (_totalGenerated + 1) + ".dcm";
                    
                    FilesCreated.Add(new FileInfo(outFileName));
                    newFile.Save(outFileName);

                    _totalGenerated++;
                }
            }
        }


        private void GenerateCorruptFile(DicomFile dFile)
        {
            // For now just remove series UID to create 'corrupt' file.
            // Should think of more creative ways to generate corrupt files which might throw the tag reader

            dFile.Dataset.Remove(DicomTag.SeriesInstanceUID);            
            dFile.Save(_outputDir + "/2018/01/01/01/CorruptFile.dcm");
        }


        public void GenerateNonDicomFile()
        {
            byte[] bytes = new byte[250 * 1024];
            _random.NextBytes(bytes);
            
            File.WriteAllBytes(_outputDir + "/2018/01/01/01/TotallyADicomFile.dcm", bytes);
            File.WriteAllText(_outputDir + "/2018/01/01/01/ARandomTextFile.dcm", bytes.ToString());
        }
        

        public void Dispose()
        {
            if(_deleteAfter)
            {
                try
                {
                    Directory.Delete(_outputDir, true);
                }
                catch(IOException e)
                {
                    Console.WriteLine("Could not dispose generated files: " + e);
                }
            }
        }
    }
}
