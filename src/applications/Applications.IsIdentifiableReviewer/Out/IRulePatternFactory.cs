using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer.Out
{
    /// <summary>
    /// Interface for classes which generate patterns for classifying <see cref="Failure"/> as true or false positives and describing which part to redact.  Can be a strategy (e.g. use whole of the input string) or involve user input (e.g. get user to type in the pattern they want).
    /// </summary>
    public interface IRulePatternFactory
    {
        /// <summary>
        /// Returns a Regex for picking up the provided <paramref name="failure"/>
        /// </summary>
        /// <param name="failure"></param>
        /// <param name="sender">The requester of the pattern</param>
        /// <returns></returns>
        string GetPattern(object sender,Failure failure);
    }
}
