﻿using System;
using System.Runtime.Serialization;

namespace Microservices.IdentifierMapper.Messaging
{
    /// <summary>
    /// Exception thrown when the PatientID tag of a dicom file contains invalid/corrupt data
    /// </summary>
    public class BadPatientIDException : Exception
    {
        public BadPatientIDException()
        {
        }

        public BadPatientIDException(string message) : base(message)
        {
        }

        public BadPatientIDException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
