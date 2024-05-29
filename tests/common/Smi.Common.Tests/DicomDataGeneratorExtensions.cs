using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BadMedicine;
using BadMedicine.Dicom;
using FellowOakDicom;
using NUnit.Framework;

namespace Smi.Common.Tests
{
    public static class DicomDataGeneratorExtensions
    {
        public static List<DicomDataset> GenerateImages(this DicomDataGenerator g, int numberOfImages,Random r)
        {
            var toReturn = new List<DicomDataset>();
            g.MaximumImages = numberOfImages;

            while (toReturn.Count <=  numberOfImages)
                toReturn.AddRange(g.GenerateStudyImages(new Person(r), out _));

            //trim off extras
            toReturn = toReturn.Take(numberOfImages).ToList();

            Assert.That(toReturn,Has.Count.EqualTo(numberOfImages));

            return toReturn;
        }

        public static IEnumerable<FileInfo> GenerateImageFiles(this DicomDataGenerator g, int numberOfImages, Random r)
        {
            var p = new PersonCollection();
            p.GeneratePeople(5000,r);

            if(g.OutputDir?.Exists==true)
                g.OutputDir.Delete(true);

            var inventory = new FileInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, "inventory.csv"));

            g.MaximumImages = numberOfImages;
            g.GenerateTestDataFile(p,inventory,numberOfImages);

            return g.OutputDir?.GetFiles("*.dcm",SearchOption.AllDirectories)??Enumerable.Empty<FileInfo>();
        }
    }
}
