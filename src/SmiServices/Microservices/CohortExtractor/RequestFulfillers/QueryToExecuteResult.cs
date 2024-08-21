namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers;

public sealed record QueryToExecuteResult(
    string FilePathValue,
    string? StudyTagValue,
    string? SeriesTagValue,
    string? InstanceTagValue,
    bool Reject,
    string? RejectReason)
{
    public override string ToString() => $"{FilePathValue}(Reject={Reject})";
}
