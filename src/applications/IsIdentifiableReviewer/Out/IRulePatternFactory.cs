using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer.Out
{
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