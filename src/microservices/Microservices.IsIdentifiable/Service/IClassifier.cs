using IsIdentifiable.Reporting;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace Microservices.IsIdentifiable.Service
{
    public interface IClassifier
    {
        /// <summary>
        /// The location in which you can get your required data files
        /// </summary>
        IDirectoryInfo DataDirectory { get; set; }

        IEnumerable<Failure> Classify(IFileInfo dcm);
    }
}
