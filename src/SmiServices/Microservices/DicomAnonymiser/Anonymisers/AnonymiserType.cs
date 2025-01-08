namespace SmiServices.Microservices.DicomAnonymiser.Anonymisers;

public enum AnonymiserType
{
    /// <summary>
    /// Unused placeholder value
    /// </summary>
    None = 0,

    DefaultAnonymiser = 1,

    SmiCtpAnonymiser = 2,
}
