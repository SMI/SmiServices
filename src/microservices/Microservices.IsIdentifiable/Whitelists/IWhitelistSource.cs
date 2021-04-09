using System;
using System.Collections.Generic;

namespace Microservices.IsIdentifiable.Whitelists
{
    public interface IWhitelistSource : IDisposable
    {
        /// <summary>
        /// Return all unique strings which should be ignored.  These strings should be trimmed.  Case is not relevant.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetWhitelist();
    }
}