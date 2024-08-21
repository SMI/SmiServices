using BadMedicine.Dicom;
using FellowOakDicom;
using NUnit.Framework;
using SynthEHR;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace SmiServices.UnitTests.Common
{
    public static class DicomDataGeneratorExtensions
    {
        public static List<DicomDataset> GenerateImages(this DicomDataGenerator g, int numberOfImages, Random r)
        {
            var toReturn = new List<DicomDataset>();
            g.MaximumImages = numberOfImages;

            while (toReturn.Count <= numberOfImages)
                toReturn.AddRange(g.GenerateStudyImages(new Person(r), out _));

            //trim off extras
            toReturn = toReturn.Take(numberOfImages).ToList();

            Assert.That(toReturn, Has.Count.EqualTo(numberOfImages));

            return toReturn;
        }

        public static IEnumerable<IFileInfo> GenerateImageFiles(this DicomDataGenerator g, int numberOfImages, Random r)
        {
            var p = new PersonCollection();
            p.GeneratePeople(5000, r);

            if (g.OutputDir?.Exists == true)
                g.OutputDir.Delete(true);

            var inventory = new FileInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, "inventory.csv"));

            g.MaximumImages = numberOfImages;
            g.GenerateTestDataFile(p, inventory, numberOfImages);

            // TODO(rkm 2024-08-21) Make DicomDataGenerator support FileSystem abstractions
            var fileSystem = new FileSystem();

            return g.OutputDir?.GetFiles("*.dcm", SearchOption.AllDirectories).Select(x => fileSystem.FileInfo.New(x.FullName)) ?? Enumerable.Empty<IFileInfo>();
        }
    }
}
