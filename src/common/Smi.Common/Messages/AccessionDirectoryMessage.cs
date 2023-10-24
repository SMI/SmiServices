
using Equ;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Smi.Common.Messages
{
    /// <summary>
    /// Object representing an accession directory message.
    /// </summary>
    public sealed class AccessionDirectoryMessage : MemberwiseEquatable<AccessionDirectoryMessage>, IMessage
    {
        /// <summary>
        /// Directory path relative to the root path.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DirectoryPath { get; set; } = null!;

        public AccessionDirectoryMessage() { }

        public AccessionDirectoryMessage(string root, DirectoryInfo directory)
        {
            if (!directory.FullName.StartsWith(root, StringComparison.CurrentCultureIgnoreCase))
                throw new Exception("Directory '" + directory + "' did not share a common root with the root '" + root + "'");

            DirectoryPath = directory.FullName.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        public string GetAbsolutePath(string rootPath) => Path.Combine(rootPath, DirectoryPath);

        public override string ToString() => $"AccessionDirectoryMessage[DirectoryPath={DirectoryPath}]";
    }
}
