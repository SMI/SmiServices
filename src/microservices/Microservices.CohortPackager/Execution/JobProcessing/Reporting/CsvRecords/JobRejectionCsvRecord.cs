using Equ;
using Microservices.CohortPackager.Execution.ExtractJobStorage;
using System;
using System.Collections.Generic;

namespace Microservices.CohortPackager.Execution.JobProcessing.Reporting.CsvRecords;

public class JobRejectionCsvRecord : MemberwiseEquatable<JobRejectionCsvRecord>, IExtractionReportCsvRecord
{
    public string RequestedUID { get; }

    public string Reason { get; }

    public uint Count { get; }


    public JobRejectionCsvRecord(string requestedUid, string reason, uint count)
    {
        RequestedUID = string.IsNullOrWhiteSpace(requestedUid) ? throw new ArgumentException($"'{nameof(requestedUid)}' cannot be null or whitespace", nameof(requestedUid)) : requestedUid;
        Reason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException($"'{nameof(reason)}' cannot be null or whitespace", nameof(reason)) : reason;
        Count = count == 0 ? throw new ArgumentException($"'{nameof(count)}' must be greater than 0", nameof(count)) : count;
    }

    public override string ToString() => $"JobRejectionCsvRecord({RequestedUID},{Reason},{Count})";

    public static IEnumerable<JobRejectionCsvRecord> FromExtractionIdentifierRejectionInfos(IEnumerable<ExtractionIdentifierRejectionInfo> rejections)
    {
        foreach (var rejection in rejections)
        {
            var requestedUid = rejection.ExtractionIdentifier;

            foreach (var rejectionItem in rejection.RejectionItems)
                yield return new JobRejectionCsvRecord(
                    requestedUid,
                    rejectionItem.Key,
                    (uint)rejectionItem.Value
                );
        }
    }
}
