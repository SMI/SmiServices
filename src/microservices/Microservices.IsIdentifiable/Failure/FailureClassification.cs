using Microservices.IsIdentifiable.Rules;

namespace Microservices.IsIdentifiable.Failure
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

        Postcode,

        /// <summary>
        /// A rule violation by an <see cref="ICustomRule"/>
        /// </summary>
        CustomRule

    }
}