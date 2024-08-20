using System.Collections.Generic;
using System.IO.Abstractions;

namespace SmiServices.Common
{
    public class ZipHelper
    {
        readonly static List<string> SupportedExtensions =
        [
            ".zip",
            ".tar"
        ];

        /// <summary>
        /// Returns true if <paramref name="f"/> looks like a compressed archive compatible with smi e.g. zip, tar etc
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static bool IsZip(IFileInfo f)
        {
            return SupportedExtensions.Contains(f.Extension);
        }
    }
}
