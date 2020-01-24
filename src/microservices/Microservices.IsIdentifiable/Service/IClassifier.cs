using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microservices.IsIdentifiable.Reporting.Reports;

namespace Microservices.IsIdentifiable.Service
{
    public interface IClassifier
    {
        IFailureReport Classify(FileInfo dcm);
    }
}
