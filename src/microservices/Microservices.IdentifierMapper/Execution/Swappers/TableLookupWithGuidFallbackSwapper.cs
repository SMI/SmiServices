using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using FAnsi.Discovery;
using NLog;
using Smi.Common.Options;

namespace Microservices.IdentifierMapper.Execution.Swappers
{
    /// <summary>
    /// Identifier swapper which connects to lookup table (wrapping <see cref="TableLookupSwapper"/>) but if no mapping is
    /// found then generates a guid substitution value (which is stored in the same database - using <see cref="ForGuidIdentifierSwapper"/>)
    ///
    /// <para>The Guid mapping table will be the mapping table name with the suffix <see cref="GuidTableSuffix"/> (i.e. <see cref="IMappingTableOptions.MappingTableName"/> + suffix)</para>
    /// </summary>
    public class TableLookupWithGuidFallbackSwapper : SwapIdentifiers
    {
        /// <summary>
        /// Determines the name to give/expect for the substitution table when the lookup misses.  The name will be
        /// the name of the lookup table + this suffix
        /// </summary>
        public const string GuidTableSuffix = "_guid";
        
        /// <summary>
        /// The name to give/expect for the <see cref="IMappingTableOptions.ReplacementColumnName"/> in the guid swap table
        /// (that stores lookup misses)
        /// </summary>
        public const string GuidColumnName = "guid";

        private readonly TableLookupSwapper _tableSwapper;
        private readonly ForGuidIdentifierSwapper _guidSwapper;

        public TableLookupWithGuidFallbackSwapper()
        {
            _tableSwapper = new TableLookupSwapper();
            _guidSwapper = new ForGuidIdentifierSwapper();
        }

        /// <inheritdoc/>
        public override void Setup(IMappingTableOptions mappingTableOptions)
        {
            _tableSwapper.Setup(mappingTableOptions);

            var guidOptions = mappingTableOptions.Clone();
            guidOptions.MappingTableName = GetGuidTable(guidOptions).GetFullyQualifiedName();
            guidOptions.ReplacementColumnName = GuidColumnName;
            _guidSwapper.Setup(guidOptions);
        }

        /// <summary>
        /// Returns a table in which guids would be stored for mapping table lookup misses.  This will be in
        /// the same database and schema as <paramref name="options"/> but the table name will have the <see cref="GuidTableSuffix"/>
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public DiscoveredTable GetGuidTable(IMappingTableOptions options)
        {
            var mappingTable = options.Discover();
            var guidTableName = mappingTable.GetRuntimeName() + GuidTableSuffix;

            return mappingTable.Database.ExpectTable(guidTableName,mappingTable.Schema,TableType.Table);
        }
         
        /// <summary>
        /// Returns a substitution from the wrapped <see cref="TableLookupSwapper"/>.  If no match is found then a guid is allocated
        /// and stored using a wrapped <see cref="ForGuidIdentifierSwapper"/>.
        /// </summary>
        /// <param name="toSwap"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public override string GetSubstitutionFor(string toSwap, out string reason)
        {
            //get answer from lookup table
            var answer = _tableSwapper.GetSubstitutionFor(toSwap, out reason);

            //if we didn't get a hit in the lookup table use the guid allocation swapper
            if (string.IsNullOrWhiteSpace(answer))
                return _guidSwapper.GetSubstitutionFor(toSwap, out reason);
            
            return answer;
        }

        /// <summary>
        /// Calls <see cref="ISwapIdentifiers.ClearCache"/> on both wrapped swappers (guid and lookup)
        /// </summary>
        public override void ClearCache()
        {
            _tableSwapper.ClearCache();
            _guidSwapper.ClearCache();
        }

        public override void LogProgress(ILogger logger, LogLevel level)
        {
            _tableSwapper.LogProgress(logger, level);
            _guidSwapper.LogProgress(logger, level);
        }
    }
}
