using IsIdentifiable.Failures;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace SmiServices.Microservices.IsIdentifiable
{
    public interface IClassifier
    {
        /// <summary>
        /// The location in which you can get your required data files
        /// </summary>
        IDirectoryInfo? DataDirectory { get; set; }

        IEnumerable<Failure> Classify(IFileInfo dcm);
    }
}
