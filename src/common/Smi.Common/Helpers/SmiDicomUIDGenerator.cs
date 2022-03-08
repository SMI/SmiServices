using Dicom;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Smi.Common.Helpers
{
    /// <summary>
    /// Generates 64-byte DICOM UID values based on
    /// https://dicom.nema.org/medical/dicom/current/output/chtml/part05/sect_B.2.html.
    /// This includes a "SMI" identifier.
    /// <para/>
    /// Note this class has the "Smi" prefix to not cause confusion with the fo-dicom <see cref="DicomUIDGenerator"/> class
    /// </summary>
    public static class SmiDicomUIDGenerator
    {
        // https://dicom.nema.org/medical/dicom/current/output/chtml/part05/sect_6.2.html
        private const string _uidCharset = "0123456789";

        private const string _derivedPrefix = "2.25.";

        // "SMI" in ASCII
        private const string _smiPrefix = "837773.";

        private static readonly int _length = 64;
        private static readonly int _postfixLength = _length - _derivedPrefix.Length - _smiPrefix.Length;


        public static string Generate()
        {
            // Possibly a bit overkill
            // https://stackoverflow.com/a/1344255/9351183

            byte[] data = new byte[4 * _postfixLength];
            using (var crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(data);
            }

            StringBuilder result = new(_length);
            result.Append(_derivedPrefix);
            result.Append(_smiPrefix);

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
