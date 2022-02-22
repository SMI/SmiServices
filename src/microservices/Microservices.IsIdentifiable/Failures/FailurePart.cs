using Equ;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microservices.IsIdentifiable.Failures
{
    public class FailurePart : MemberwiseEquatable<FailurePart>
    {
        /// <summary>
        /// The classification of the failure e.g. CHI, PERSON, TextInPixel
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public FailureClassification Classification { get; set; }

        /// <summary>
        /// The location in a string in which the ProblemValue appeared.
        /// 
        /// <para>-1 if not appropriate (e.g. text detected in an image)</para>
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// The word that failed validation
        /// </summary>
        public string Word { get; set; }

        public FailurePart(string word,FailureClassification classification,int offset =-1)
        {
            Word = word;
            Classification = classification;
            Offset = offset;
        }

        /// <summary>
        /// Returns true if the provided <paramref name="index"/> is within the problem part of the original string
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool Includes(int index)
        {
            if (Offset == -1)
                return false;

            if (string.IsNullOrWhiteSpace(Word))
                return false;

            return index >= Offset && index < Offset + Word.Length;
        }
        
        /// <summary>
        /// Returns true if the failure part includes ANY of the indexes between start and start+length
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public bool Includes(int start, int length)
        {
            for (int i = start; i < start + length; i++)
                if(Includes(i))
                    return true;

            return false;
        }
    }
}