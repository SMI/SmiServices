using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.Oracle;
using FAnsi.Implementations.PostgreSql;
using Microservices.IdentifierMapper.Execution.Swappers;
using Smi.Common.Helpers;
using Smi.Common.Messages.Updating;
using Smi.Common.Options;
using System;

namespace TriggerUpdates.Execution
{
    public class MapperSource : ITriggerUpdatesSource
    {
        private ISwapIdentifiers _swapper;

        public MapperSource(GlobalOptions globalOptions, TriggerUpdatesFromMapperOptions cliOptions)
        {
            ImplementationManager.Load<MySqlImplementation>();
            ImplementationManager.Load<OracleImplementation>();
            ImplementationManager.Load<MicrosoftSQLImplementation>();
            ImplementationManager.Load<PostgreSqlImplementation>();

            try
            {
                var objectFactory = new MicroserviceObjectFactory();
                _swapper = objectFactory.CreateInstance<ISwapIdentifiers>(globalOptions.IdentifierMapperOptions.SwapperType, typeof(ISwapIdentifiers).Assembly);
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"Could not create IdentifierMapper Swapper with SwapperType:{globalOptions?.IdentifierMapperOptions?.SwapperType ?? "Null"}",ex);
            }

            if(_swapper == null)
                throw new ArgumentException("No SwapperType has been specified in GlobalOptions.IdentifierMapperOptions");
            
        }

        public UpdateValuesMessage Next()
        {
            if(!(_swapper is TableLookupWithGuidFallbackSwapper guidSwapper))
                throw new NotSupportedException("Currently only TableLookupWithGuidFallbackSwapper is supported by this class");

            return null;
        }
    }
}