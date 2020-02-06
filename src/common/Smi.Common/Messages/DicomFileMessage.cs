
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Smi.Common.Messages
{
    /// <inheritdoc />
    /// <summary>
    /// Object representing a dicom file message.
    /// https://github.com/HicServices/SMIPlugin/wiki/SMI-RabbitMQ-messages-and-queues#dicomfilemessage
    /// </summary>
    public sealed class DicomFileMessage : IComparable, IFileReferenceMessage
    {
        /// <summary>
        /// NationalPACSAccessionNumber obtained from the end of the directory path.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string NationalPACSAccessionNumber { get; set; }

        /// <summary>
        /// File path relative to the root path.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DicomFilePath { get; set; }

        public long DicomFileSize { get; set; } = -1;

        /// <summary>
        /// Dicom tag (0020,000D).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string StudyInstanceUID { get; set; }

        /// <summary>
        /// Dicom tag (0020,000E).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SeriesInstanceUID { get; set; }

        /// <summary>
        /// Dicom tag (0008,0018)
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SOPInstanceUID { get; set; }

        /// <summary>
        /// Key-value pairs of Dicom tags and their values.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DicomDataset { get; set; }


        public DicomFileMessage() { }

        public DicomFileMessage(string root, FileInfo file)
            : this(root, file.FullName) { }

        public DicomFileMessage(string root, string file)
        {
            if (!file.StartsWith(root, StringComparison.CurrentCultureIgnoreCase))
                throw new Exception("File '" + file + "' did not share a common root with the root '" + root + "'");

            DicomFilePath = file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        public string GetAbsolutePath(string rootPath)
        {
            return Path.Combine(rootPath, DicomFilePath);
        }

        public bool Validate(string fileSystemRoot)
        {
            var absolutePath = GetAbsolutePath(fileSystemRoot);

            if (string.IsNullOrWhiteSpace(absolutePath))
                return false;

            try
            {
                var dir = new FileInfo(absolutePath);

                //There file referenced must exist 
                return dir.Exists;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public bool VerifyPopulated()
        {
            return !string.IsNullOrWhiteSpace(NationalPACSAccessionNumber) &&
                   !string.IsNullOrWhiteSpace(DicomFilePath) &&
                   !string.IsNullOrWhiteSpace(StudyInstanceUID) &&
                   !string.IsNullOrWhiteSpace(SeriesInstanceUID) &&
                   !string.IsNullOrWhiteSpace(SOPInstanceUID) &&
                   !string.IsNullOrWhiteSpace(DicomDataset);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("NationalPACSAccessionNumber: " + NationalPACSAccessionNumber);
            sb.AppendLine("DicomFilePath: " + DicomFilePath);
            sb.AppendLine("StudyInstanceUID: " + StudyInstanceUID);
            sb.AppendLine("SeriesInstanceUID: " + SeriesInstanceUID);
            sb.AppendLine("SOPInstanceUID: " + SOPInstanceUID);
            sb.AppendLine("=== DicomDataset ===\n" + DicomDataset + "\n====================");

            return sb.ToString();
        }

        #region Equality Members

        private bool Equals(DicomFileMessage other)
        {
            return
                string.Equals(NationalPACSAccessionNumber, other.NationalPACSAccessionNumber)
                && string.Equals(DicomFilePath, other.DicomFilePath)
                && string.Equals(StudyInstanceUID, other.StudyInstanceUID)
                && string.Equals(SeriesInstanceUID, other.SeriesInstanceUID)
                && string.Equals(SOPInstanceUID, other.SOPInstanceUID)
                && string.Equals(DicomDataset, other.DicomDataset);
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is DicomFileMessage && Equals((DicomFileMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (NationalPACSAccessionNumber != null ? NationalPACSAccessionNumber.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DicomFilePath != null ? DicomFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (StudyInstanceUID != null ? StudyInstanceUID.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SeriesInstanceUID != null ? SeriesInstanceUID.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SOPInstanceUID != null ? SOPInstanceUID.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DicomDataset != null ? DicomDataset.GetHashCode() : 0);
                return hashCode;
            }
        }

        public int CompareTo(object obj)
        {
            return string.Compare(DicomFilePath, ((DicomFileMessage)obj).DicomFilePath, StringComparison.Ordinal);
        }

        public static bool operator ==(DicomFileMessage left, DicomFileMessage right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DicomFileMessage left, DicomFileMessage right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}

