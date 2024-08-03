using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using IsIdentifiable.Failures;

namespace SmiServices.Microservices.IsIdentifiable
{
    public interface IClassifier
    {
        /// <summary>
        /// The location in which you can get your required data files
        /// </summary>
        DirectoryInfo? DataDirectory { get; set; }

        IEnumerable<Failure> Classify(IFileInfo dcm);
    }
}
