using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace Smi.Common
{
    public class ZipHelper
    {
        readonly static List<string> SupportedExtensions = new()
        {
            ".zip",
            ".tar"
        };

        /// <summary>
        /// Returns true if <paramref name="f"/> looks like a compressed archive compatible with smi e.g. zip, tar etc
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static bool IsZip(IFileInfo f)
        {
            return SupportedExtensions.Contains(f.Extension);
        }

        /// <summary>
        /// Returns true if <paramref name="path"/> looks like a compressed archive compatible with smi e.g. zip, tar etc
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsZip(string path)
        {
            return SupportedExtensions.Contains(Path.GetExtension(path));
        }

    }
}
