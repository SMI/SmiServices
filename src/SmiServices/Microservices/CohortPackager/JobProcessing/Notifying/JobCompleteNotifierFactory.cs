using System;


namespace SmiServices.Microservices.CohortPackager.JobProcessing.Notifying;

public static class JobCompleteNotifierFactory
{
    public static IJobCompleteNotifier GetNotifier(
        string notifierTypeStr
    )
    {
        return notifierTypeStr switch
        {
            nameof(LoggingNotifier) => new LoggingNotifier(),
            _ => throw new ArgumentException($"No case for type, or invalid type string '{notifierTypeStr}'")
        };
    }
}
