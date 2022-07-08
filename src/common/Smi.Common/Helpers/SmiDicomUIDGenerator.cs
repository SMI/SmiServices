using Dicom;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Smi.Common.Helpers
{
    /// <summary>
    /// Generates 64-byte DICOM UID values based on a cryptographic random number generator.
    /// The generated UIDs will always start with the same prefix, consisting of the "2.25." root as specified by
    /// https://dicom.nema.org/medical/dicom/current/output/chtml/part05/sect_B.2.html, and a specified prefix.
    /// <para/>
    /// Note this class has the "Smi" prefix to not cause confusion with the fo-dicom <see cref="DicomUIDGenerator"/> class
    /// </summary>
    public class SmiDicomUIDGenerator
    {
        public const int DICOM_UID_MAX_LENGTH = 64;
        public const string DICOM_DERIVED_UID_PREFIX = "2.25.";

        // https://dicom.nema.org/medical/dicom/current/output/chtml/part05/sect_6.2.html
        private const string _uidCharset = "0123456789";

        // The "derived UID" prefix, plus "SMI1" in ASCII
        //private const string _prefix = "2.25.83777349.";

        private readonly string _prefix;
        private readonly int _postfixLength;

        /// <summary>
        /// Create a SmiDicomUIDGenerator which will generate UIDs using the base prefix and the suppliedPrefix
        /// </summary>
        /// <param name="suppliedPrefix"></param>
        /// <exception cref="ArgumentException"></exception>
        public SmiDicomUIDGenerator(string suppliedPrefix)
        {
            if (!suppliedPrefix.All(char.IsDigit))
                throw new ArgumentException("Specified prefix must only contain digits");

            _prefix = $"{DICOM_DERIVED_UID_PREFIX}{suppliedPrefix}.";
            _postfixLength = DICOM_UID_MAX_LENGTH - _prefix.Length;

            if (_postfixLength < 1)
                throw new ArgumentException("Specified prefix is too long");
        }

        /// <summary>
        /// Generate a valid DICOM UID string using the stored prefix.
        /// </summary>
        /// <returns></returns>
        public string Generate()
        {
            // Possibly a bit overkill
            // Ref: https://stackoverflow.com/a/1344255/9351183

            byte[] data = new byte[4 * _postfixLength];

            using (var crypto = RandomNumberGenerator.Create())
                crypto.GetBytes(data);

            StringBuilder result = new(DICOM_UID_MAX_LENGTH);
            result.Append(_prefix);

            for (int i = 0; i < _postfixLength; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = (int)(rnd % _uidCharset.Length);

                result.Append(_uidCharset[idx]);
            }

            return result.ToString();
        }
    }
}
