namespace Microservices.IsIdentifiable.Failures
{
    public enum FailureClassification
    {
        None = 0,
        /// <summary>
        /// e.g. CHI number 
        /// </summary>
        PrivateIdentifier,
        Location,
        Person,
        Organization,
        Money,
        Percent,
        Date,
        Time,

        /// <summary>
        /// Word(s) found in pixel data using OCR.
        /// </summary>
        PixelText,

        Postcode
    }
}