using System;

namespace Microservices.IsIdentifiable.Failures
{
    public class FailurePart : IEquatable<FailurePart>
    {
        /// <summary>
        /// The classification of the failure e.g. CHI, PERSON, TextInPixel
        /// </summary>
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
        
        #region Equality
        public bool Equals(FailurePart other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Offset == other.Offset && Word == other.Word;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FailurePart) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Offset * 397) ^ (Word != null ? Word.GetHashCode() : 0);
            }
        }
        #endregion
    }
}