
using Dicom;
using DicomTypeTranslation;
using Microservices.Common.Tests;
using FAnsi.Discovery.TypeTranslation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rdmp.Dicom.TagPromotionSchema;
using TypeGuesser;

namespace Microservices.Tests.RDMPTests.TestTagData
{
    public class TestTagDataGenerator : ITagRandomiser
    {
        public bool FreakyCharactersAllowed = false;
        private string[] _cachedAnswer;

        public DicomDictionaryEntry GetRandomTag(Random r)
        {
            string[] availableTags = GetAvailableTags();

            int i = r.Next(availableTags.Length);

            return TagColumnAdder.GetTag(availableTags[i]);
        }

        private string[] GetAvailableTags()
        {
            return _cachedAnswer ?? (_cachedAnswer = TagColumnAdder.GetAvailableTags());
        }

        public object GetRandomValue(DicomDictionaryEntry dicomDictionaryEntry, Random r)
        {
            if (dicomDictionaryEntry.ValueRepresentations.First() != dicomDictionaryEntry.Tag.DictionaryEntry.ValueRepresentations.First())
                return null;

            if (dicomDictionaryEntry.ValueRepresentations.Any(vr => vr == DicomVR.UN)) //unknown
                return null;

            if (dicomDictionaryEntry.ValueRepresentations.Any(vr => vr == DicomVR.AT))
                return null;

            if (dicomDictionaryEntry.ValueRepresentations.Any(vr => vr == DicomVR.SQ))
                return GetRandomSequence(dicomDictionaryEntry, r, r.Next(3));

            if (dicomDictionaryEntry.ValueMultiplicity.Maximum > 1)
            {
                //don't create more than 100 elments!

                var max = dicomDictionaryEntry.ValueMultiplicity.Maximum;
                var min = dicomDictionaryEntry.ValueMultiplicity.Minimum;

                //if the maximum multiplicity is more than 100
                if (max > 100)
                    max = Math.Max(min, Math.Min(100, max)); //set the max to 100 (unless min is already more than 100 in which case set to that)

                int elmentsToCreate = r.Next(min, max + 1);

                var natural = DicomTypeTranslater.GetNaturalTypeForVr(dicomDictionaryEntry.ValueRepresentations.First(), new DicomVM(0, 1, 1));

                var toReturn = Array.CreateInstance(natural.CSharpType, elmentsToCreate);
                for (int i = 0; i < elmentsToCreate; i++)
                {
                    object toAddValue = null;

                    int maxAttempts = 100;

                    while (toAddValue == null || (toAddValue is string && string.IsNullOrWhiteSpace((string)toAddValue)))
                    {
                        maxAttempts--;
                        toAddValue = GetRandomValue(natural, r);

                        if (maxAttempts <= 0)
                            return null;//no array for you this type is all dodgy whatever it is
                    }

                    toReturn.SetValue(toAddValue, i);
                }

                return toReturn;
            }
            if (dicomDictionaryEntry.ValueRepresentations.Contains(DicomVR.NONE))
                return null;

            DatabaseTypeRequest naturalType = DicomTypeTranslater.GetNaturalTypeForVr(dicomDictionaryEntry.ValueRepresentations.First(), dicomDictionaryEntry.ValueMultiplicity);
            object randValue = GetRandomValue(naturalType, r);

            return randValue;
        }

        private Dictionary<DicomTag, object>[] GetRandomSequence(DicomDictionaryEntry sequenceTag, Random r, int maximumRecursions)
        {
            var toReturn = new Dictionary<DicomTag, object>();

            maximumRecursions--;

            if (maximumRecursions <= 0)
                return null;

            int toGenerate = r.Next(20);
            for (int i = 0; i < toGenerate; i++)
            {
                var toAddTag = GetRandomTag(r);

                //once we are down the maximum level of recursions stop generating sequences! (Seems the dicom standard is mostly composed of sequences)
                while (maximumRecursions > 0 && toAddTag.ValueRepresentations.Contains(DicomVR.SQ))
                    toAddTag = GetRandomTag(r);

                var toAddValue = GetRandomValue(toAddTag, r);

                //don't add nulls or empty strings
                if (toAddValue == null || (toAddValue is string && string.IsNullOrWhiteSpace((string)toAddValue)))
                    continue;

                //don't double add
                if (toReturn.ContainsKey(toAddTag.Tag))
                    continue;

                toReturn.Add(toAddTag.Tag, toAddValue);
            }

            // Return a sequence with a single element 
            return new[] { toReturn };
        }

        private object GetRandomValue(DatabaseTypeRequest requestedType, Random r)
        {
            if (requestedType.CSharpType == typeof(string))
            {
                if (!requestedType.Width.HasValue)
                    throw new Exception("Expected DatabaseTypeRequest to have a MaxWidthForStrings");

                return GetRandomString(requestedType.Width.Value, r);
            }

            if (requestedType.CSharpType == typeof(bool))
                return r.Next(2) == 1;

            if (requestedType.CSharpType == typeof(int))
                return r.Next(int.MaxValue);

            if (requestedType.CSharpType == typeof(short))
                return (short)r.Next(short.MaxValue);

            if (requestedType.CSharpType == typeof(ushort))
                return (ushort)r.Next(ushort.MaxValue);

            if (requestedType.CSharpType == typeof(decimal))
                return (decimal)r.NextDouble();

            if (requestedType.CSharpType == typeof(float))
                return (float)r.NextDouble();

            if (requestedType.CSharpType == typeof(double))
                return r.NextDouble();

            if (requestedType.CSharpType == typeof(long))
                return (long)r.Next(int.MaxValue);

            if (requestedType.CSharpType == typeof(uint))
                return (uint)r.Next(int.MaxValue);

            if (requestedType.CSharpType == typeof(TimeSpan))
                return new TimeSpan(0, 0, 0, r.Next(86400));

            if (requestedType.CSharpType == typeof(DateTime)) //ironically Convert.ToDateTime likes int and floats as valid dates -- nuts
                return GetRandomDay(r);

            return null;
        }

        private string GetRandomString(int maxLength, Random r)
        {
            StringBuilder sb = new StringBuilder();

            int lengthToGenerate = 1 + r.Next(Math.Min(maxLength, 8001));

            for (int i = 0; i < lengthToGenerate; i++)
            {
                if (FreakyCharactersAllowed)
                    sb.Append((char)r.Next(char.MinValue, char.MaxValue));
                else
                    sb.Append(GetRandomCharacterNonFreaky(r));
            }

            if (sb.Length == 0)
                return null;
            var toReturn = sb.ToString();
            return toReturn;
        }

        private char GetRandomCharacterNonFreaky(Random r)
        {
            string chars = "$%#@!*abcdefghijklmnopqrstuvwxyz1234567890?;:ABCDEFGHIJKLMNOPQRSTUVWXYZ^&";
            int num = r.Next(0, chars.Length - 1);
            return chars[num];
        }

        private DateTime GetRandomDay(Random r)
        {
            DateTime start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;
            return start.AddDays(r.Next(range));
        }
    }
}
