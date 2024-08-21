using System;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers;

public sealed record QueryToExecuteResult
{
    public QueryToExecuteResult(string FilePathValue,
        string? StudyTagValue,
        string? SeriesTagValue,
        string? InstanceTagValue,
        bool Reject,
        string? RejectReason)
    {
        if (Reject && string.IsNullOrWhiteSpace(RejectReason)) throw new ArgumentException("RejectReason must be provided when Reject is true",nameof(RejectReason));

        this.FilePathValue = FilePathValue;
        this.StudyTagValue = StudyTagValue;
        this.SeriesTagValue = SeriesTagValue;
        this.InstanceTagValue = InstanceTagValue;
        this.Reject = Reject;
        this.RejectReason = RejectReason;
    }

    public override string ToString() => $"{FilePathValue}(Reject={Reject})";
    public string FilePathValue { get; init; }
    public string? StudyTagValue { get; init; }
    public string? SeriesTagValue { get; init; }
    public string? InstanceTagValue { get; init; }
    public bool Reject { get; init; }
    public string? RejectReason { get; init; }
}
