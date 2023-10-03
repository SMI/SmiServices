using System;
using System.Collections.ObjectModel;
using System.Linq;
using Rdmp.Core.Curation.Data;

namespace Microservices.CohortExtractor.Execution.RequestFulfillers
{
    public class QueryToExecuteColumnSet
    {
        public const string DefaultImagePathColumnName = "RelativeFileArchiveURI";
        public const string DefaultStudyIdColumnName = "StudyInstanceUID";
        public const string DefaultSeriesIdColumnName = "SeriesInstanceUID";
        public const string DefaultInstanceIdColumnName = "SOPInstanceUID";

        /// <summary>
        /// The dataset to query
        /// </summary>
        public readonly ICatalogue Catalogue;

        /// <summary>
        /// The column in the <see cref="Catalogue"/> that stores the location on disk of the image
        /// </summary>
        public readonly ExtractionInformation? FilePathColumn;

        /// <summary>
        /// The column in the <see cref="Catalogue"/> that stores the StudyInstanceUID
        /// </summary>
        public readonly ExtractionInformation? StudyTagColumn;

        /// <summary>
        /// The column in the <see cref="Catalogue"/> that stores the SeriesInstanceUID
        /// </summary>
        public readonly ExtractionInformation? SeriesTagColumn;

        /// <summary>
        /// The column in the <see cref="Catalogue"/> that stores the SOPInstanceUID
        /// </summary>
        public readonly ExtractionInformation? InstanceTagColumn;

        /// <summary>
        /// All the extractable columns in the <see cref="Catalogue"/> (includes <see cref="SeriesTagColumn"/>, <see cref="StudyTagColumn"/> etc)
        /// </summary>
        public readonly ReadOnlyCollection<ExtractionInformation> AllColumns;

        public bool HasAllUIDs => StudyTagColumn != null && SeriesTagColumn != null && InstanceTagColumn != null;

        public QueryToExecuteColumnSet(ICatalogue catalogue,
            ExtractionInformation? filePathColumn,
            ExtractionInformation? studyTagColumn,
            ExtractionInformation? seriesTagColumn,
            ExtractionInformation? instanceTagColumn,
            bool requireFilePath = true)
        {
            Catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));

            AllColumns = new ReadOnlyCollection<ExtractionInformation>(Catalogue.GetAllExtractionInformation(ExtractionCategory.Any)); Catalogue.GetAllExtractionInformation(ExtractionCategory.Any);

            if (filePathColumn == null && requireFilePath)
                throw new ArgumentNullException(nameof(filePathColumn));

            FilePathColumn = filePathColumn;
            StudyTagColumn = studyTagColumn;
            SeriesTagColumn = seriesTagColumn;
            InstanceTagColumn = instanceTagColumn;
        }

        /// <summary>
        /// Generates a column set based on columns found in <paramref name="catalogue"/> (using the default expected column names
        /// e.g. <see cref="DefaultSeriesIdColumnName"/>).  Returns null if the <paramref name="catalogue"/> does not have all the required
        /// columns
        /// </summary>
        /// <param name="catalogue"></param>
        public static QueryToExecuteColumnSet? Create(ICatalogue catalogue)
        {
            return Create(catalogue, true);
        }

        /// <summary>
        /// Generates a column set based on columns found in <paramref name="catalogue"/> (using the default expected column names
        /// e.g. <see cref="DefaultSeriesIdColumnName"/>).  Returns null if the <paramref name="catalogue"/> does not have all the required
        /// columns
        /// </summary>
        /// <param name="catalogue"></param>
        /// <param name="requireFilePath"></param>
        public static QueryToExecuteColumnSet? Create(ICatalogue catalogue, bool requireFilePath)
        {
            if (catalogue == null)
                throw new ArgumentNullException(nameof(catalogue));

            var eis = catalogue.GetAllExtractionInformation(ExtractionCategory.Any);

            var filePathColumn = eis.SingleOrDefault(ei => ei.GetRuntimeName().Equals(DefaultImagePathColumnName, StringComparison.CurrentCultureIgnoreCase));
            var studyTagColumn = eis.SingleOrDefault(ei => ei.GetRuntimeName().Equals(DefaultStudyIdColumnName, StringComparison.CurrentCultureIgnoreCase));
            var seriesTagColumn = eis.SingleOrDefault(ei => ei.GetRuntimeName().Equals(DefaultSeriesIdColumnName, StringComparison.CurrentCultureIgnoreCase));
            var instanceTagColumn = eis.SingleOrDefault(ei => ei.GetRuntimeName().Equals(DefaultInstanceIdColumnName, StringComparison.CurrentCultureIgnoreCase));

            if (filePathColumn == null && requireFilePath)
                return null;

            return new QueryToExecuteColumnSet(catalogue, filePathColumn, studyTagColumn, seriesTagColumn, instanceTagColumn, requireFilePath);

        }

        /// <summary>
        /// Returns true if the <see cref="Catalogue"/> contains an extractable column with the given <paramref name="column"/>
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public bool Contains(string column)
        {
            return AllColumns.Any(c => c.GetRuntimeName().Equals(column, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
