using System;
using System.Collections.Generic;
using FAnsi.Discovery;
using FAnsi.Discovery.QuerySyntax;
using Microservices.IsIdentifiable.Reporting;
using Microservices.IsIdentifiable.Rules;

namespace IsIdentifiableReviewer.Out.UpdateStrategies
{
    /// <summary>
    /// Abstract implementation of <see cref="IUpdateStrategy"/>, generates SQL statements for redacting a database
    /// </summary>
    public abstract class UpdateStrategy : IUpdateStrategy
    {
        /// <summary>
        /// Override to generate one or more SQL statements that will fully redact a given <see cref="Failure"/> in the <paramref name="table"/>
        /// </summary>
        /// <param name="table"></param>
        /// <param name="primaryKeys"></param>
        /// <param name="failure"></param>
        /// <param name="usingRule"></param>
        /// <returns></returns>
        public abstract IEnumerable<string> GetUpdateSql(DiscoveredTable table,Dictionary<DiscoveredTable, DiscoveredColumn> primaryKeys, Failure failure, IsIdentifiableRule usingRule);

        /// <summary>
        /// Returns SQL to update a single <paramref name="word"/> in the <paramref name="table"/> row referenced by the primary key value
        /// in <paramref name="failure"/>
        /// </summary>
        /// <param name="table"></param>
        /// <param name="primaryKeys"></param>
        /// <param name="syntax"></param>
        /// <param name="failure"></param>
        /// <param name="word">The word or collection of words that should be redacted</param>
        /// <returns></returns>
        protected string GetUpdateWordSql(DiscoveredTable table,
            Dictionary<DiscoveredTable, DiscoveredColumn> primaryKeys, IQuerySyntaxHelper syntax, Failure failure,string word)
        {
            if(string.IsNullOrEmpty(failure.ResourcePrimaryKey))
                throw new ArgumentException("Failure record's primary key is blank, cannot update database");

            return $@"update {table.GetFullyQualifiedName()} 
                SET {syntax.EnsureWrapped(failure.ProblemField)} = 
                REPLACE({syntax.EnsureWrapped(failure.ProblemField)},'{syntax.Escape(word)}', 'SMI_REDACTED')
                WHERE {primaryKeys[table].GetFullyQualifiedName()} = '{syntax.Escape(failure.ResourcePrimaryKey)}'";
        }
    }
}
