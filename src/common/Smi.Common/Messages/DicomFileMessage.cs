
using Equ;
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
    public sealed class DicomFileMessage : MemberwiseEquatable<DicomFileMessage>, IFileReferenceMessage
    {
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
            // Assume that only WinNT is case-insensitive, not entirely accurate but better than assuming everything is...
            if (!file.StartsWith(root, Environment.OSVersion.Platform==PlatformID.Win32NT ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture))
                throw new Exception($"File '{file}' did not share a common root with the root '{root}'");

            DicomFilePath = file[root.Length..].TrimStart(Path.DirectorySeparatorChar);
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
            return !string.IsNullOrWhiteSpace(DicomFilePath) &&
                   !string.IsNullOrWhiteSpace(StudyInstanceUID) &&
                   !string.IsNullOrWhiteSpace(SeriesInstanceUID) &&
                   !string.IsNullOrWhiteSpace(SOPInstanceUID) &&
                   !string.IsNullOrWhiteSpace(DicomDataset);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"DicomFilePath: {DicomFilePath}");
            sb.AppendLine($"StudyInstanceUID: {StudyInstanceUID}");
            sb.AppendLine($"SeriesInstanceUID: {SeriesInstanceUID}");
            sb.AppendLine($"SOPInstanceUID: {SOPInstanceUID}");
            sb.AppendLine($"=== DicomDataset ===\n{DicomDataset}\n====================");

            return sb.ToString();
        }
    }
}
