namespace Microservices.IsIdentifiable.Failures
{
    public class FailurePart
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
    }
}