using System;
using System.Collections.Generic;
using System.Linq;

namespace Smi.Common.Helpers
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Break a list of items into chunks of a specific size. Ref: https://stackoverflow.com/a/6362642
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (chunkSize < 1)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Size must be greater than 0");

            source = source.ToList();
            var pos = 0;
            while (source.Skip(pos).Any())
            {
                yield return source.Skip(pos).Take(chunkSize);
                pos += chunkSize;
            }
        }
    }
}
