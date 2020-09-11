
using Newtonsoft.Json;
using System;
using System.IO;

namespace Smi.Common.Messages
{
    /// <summary>
    /// Object representing an accession directory message.
    /// </summary>
    public sealed class AccessionDirectoryMessage : IMessage
    {
        /// <summary>
        /// NationalPACSAccessionNumber obtained from the end of the directory path.
        /// </summary>
        [JsonProperty(Required = Required.AllowNull)]
        public string NationalPACSAccessionNumber { get; set; }

        /// <summary>
        /// Directory path relative to the root path.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DirectoryPath { get; set; }


        public AccessionDirectoryMessage() { }

        public AccessionDirectoryMessage(string root, DirectoryInfo directory)
        {
            if (!directory.FullName.StartsWith(root, StringComparison.CurrentCultureIgnoreCase))
                throw new Exception("Directory '" + directory + "' did not share a common root with the root '" + root + "'");

            DirectoryPath = directory.FullName.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        public string GetAbsolutePath(string rootPath) => Path.Combine(rootPath, DirectoryPath);

        public override string ToString() => $"AccessionDirectoryMessage[NationalPACSAccessionNumber={NationalPACSAccessionNumber},DirectoryPath={DirectoryPath}]";

        #region Equality Members

        private bool Equals(AccessionDirectoryMessage other)
        {
            return string.Equals(NationalPACSAccessionNumber, other.NationalPACSAccessionNumber) && string.Equals(DirectoryPath, other.DirectoryPath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is AccessionDirectoryMessage && Equals((AccessionDirectoryMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((NationalPACSAccessionNumber != null ? NationalPACSAccessionNumber.GetHashCode() : 0) * 397) ^ (DirectoryPath != null ? DirectoryPath.GetHashCode() : 0);
            }
        }

        public static bool operator ==(AccessionDirectoryMessage left, AccessionDirectoryMessage right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AccessionDirectoryMessage left, AccessionDirectoryMessage right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
