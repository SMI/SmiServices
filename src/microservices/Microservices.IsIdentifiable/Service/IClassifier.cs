using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Text;
using Microservices.IsIdentifiable.Reporting.Reports;

namespace Microservices.IsIdentifiable.Service
{
    public interface IClassifier
    {
        /// <summary>
        /// The location in which you can get your required data files
        /// </summary>
        DirectoryInfo DataDirectory { get; set; }

        IEnumerable<Reporting.Failure> Classify(IFileInfo dcm);
    }
}
