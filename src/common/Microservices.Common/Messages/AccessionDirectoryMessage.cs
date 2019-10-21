
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Microservices.Common.Messages
{
    /// <summary>
    /// Object representing an accession directory message.
    /// https://github.com/HicServices/SMIPlugin/wiki/SMI-RabbitMQ-messages-and-queues#accessiondirectorymessage
    /// </summary>
    public sealed class AccessionDirectoryMessage : IMessage
    {
        /// <summary>
        /// NationalPACSAccessionNumber obtained from the end of the directory path.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
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

        public string GetAbsolutePath(string rootPath)
        {
            return Path.Combine(rootPath, DirectoryPath);
        }
        /// <summary>
        /// Confirms that the directory referenced by this message exists and has files/subdirectories in it.
        /// </summary>
        /// <param name="rootPath">The absolute root path to which DirectoryPath property is relative</param>
        /// <returns>true if the directory exists (relative to the rootPath) or false if it doesn't exist, is empty, contains illegal characters etc</returns>
        public bool Validate(string rootPath)
        {
            var absolutePath = GetAbsolutePath(rootPath);

            if (string.IsNullOrWhiteSpace(absolutePath))
                return false;

            try
            {
                var dir = new DirectoryInfo(absolutePath);

                //There directory referenced must exist 
                return dir.Exists && (dir.EnumerateFiles().Any() || dir.EnumerateDirectories().Any());

            }
            catch (Exception)
            {
                return false;
            }
        }

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
