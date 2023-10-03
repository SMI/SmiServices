
using System.Diagnostics.CodeAnalysis;
using FAnsi.Discovery;
using NLog;
using Smi.Common.Options;

namespace Microservices.IdentifierMapper.Execution.Swappers
{
    public interface ISwapIdentifiers
    {
        /// <summary>
        /// Setup the swapper
        /// </summary>
        /// <param name="mappingTableOptions"></param>
        void Setup(IMappingTableOptions mappingTableOptions);

        /// <summary>
        /// Returns the substitution identifier for toSwap or the reason why no substitution is possible
        /// </summary>
        /// <param name="toSwap"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        string? GetSubstitutionFor(string toSwap, out string? reason);

        /// <summary>
        /// Clear the mapping cache (if exists) and reload
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Report on the current number of swapped identifiers
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="level"></param>
        void LogProgress(ILogger logger, LogLevel level);

        /// <summary>
        /// If there is a map table based on <see cref="ForGuidIdentifierSwapper"/> schema then this should return the <see cref="DiscoveredTable"/> pointer to that table.  Otherwise should return null
        /// </summary>
        /// <returns></returns>
        DiscoveredTable? GetGuidTableIfAny(IMappingTableOptions options);
    }
}
