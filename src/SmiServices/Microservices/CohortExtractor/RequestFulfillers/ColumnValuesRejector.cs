using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace SmiServices.Microservices.CohortExtractor.RequestFulfillers
{
    public class ColumnValuesRejector : IRejector
    {
        private readonly HashSet<string> _rejectPatients;
        private readonly string _columnToCheck;

        public ColumnValuesRejector(string column, HashSet<string> values)
        {
            _columnToCheck = column;
            _rejectPatients = values;
        }

        public bool Reject(IDataRecord row, [NotNullWhen(true)] out string? reason)
        {
            string patientId;

            try
            {
                // The patient ID is null
                if (row[_columnToCheck] == DBNull.Value)
                {
                    reason = null;
                    return false;
                }

                patientId = (string)row[_columnToCheck];
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new IndexOutOfRangeException($"An error occurred determining the identifier of the record(s) being extracted. Expected a column called {_columnToCheck}", ex);
            }

            if (_rejectPatients.Contains(patientId))
            {
                reason = "Patient or Identifier was in reject list";
                return true;
            }

            reason = null;
            return false;
        }

    }
}
