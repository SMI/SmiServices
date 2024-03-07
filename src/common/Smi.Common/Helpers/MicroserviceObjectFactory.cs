using System;
using System.Reflection;
using Smi.Common.Events;
using NLog;
using Rdmp.Core.Repositories.Construction;

namespace Smi.Common.Helpers
{
    public class MicroserviceObjectFactory
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// Method called when <see cref="CreateInstance{T}(System.Type,object[])"/> fails.  If not set then the Exception is simply
        /// thrown.
        /// </summary>
        public HostFatalHandler? FatalHandler;

        /// <summary>
        /// Constructs an instance of the specified <paramref name="toCreate"/> and casts it to Type T (e.g. an interface).  You can pass any 
        /// required or optional objects required for invoking the class constructor in via <paramref name="optionalConstructorParameters"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toCreate"></param>
        /// <param name="optionalConstructorParameters"></param>
        /// <returns></returns>
        public T? CreateInstance<T>(Type toCreate, params object[] optionalConstructorParameters)
        {
            T? toReturn = default;

            try
            {
                toReturn = (T)ObjectConstructor.ConstructIfPossible(toCreate, optionalConstructorParameters);

                if (optionalConstructorParameters.Length > 0 && toReturn == null)
                    toReturn = (T)ObjectConstructor.Construct(toCreate); // Try blank constructor

                if (toReturn == null)
                    throw new Exception("ConstructIfPossible returned null");

                _logger.Info($"Successfully constructed Type '{toReturn.GetType()}'");
            }
            catch (Exception e)
            {
                _logger.Error(e,$"Failed to construct Type '{typeof(T)}'");

                if(FatalHandler != null)
                    FatalHandler(this,new FatalErrorEventArgs($"Error constructing Type {toCreate}", e));
                else
                    throw;
            }

            return toReturn;
        }

        /// <summary>
        /// Constructs an instance of the specified <paramref name="typeName"/> in the specified Assembly and casts it to Type T (e.g. an interface).
        /// You can pass any required or optional objects required for invoking the class constructor in via <paramref name="optionalConstructorParameters"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeName"></param>
        /// <param name="assembly"></param>
        /// <param name="optionalConstructorParameters"></param>
        /// <returns></returns>
        public T? CreateInstance<T>(string typeName, Assembly assembly, params object[] optionalConstructorParameters)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                _logger.Warn($"No Type name specified for T {typeof(T).Name}");
                return default;
            }

            Type toCreate = assembly.GetType(typeName, true) ?? throw new Exception($"Could not create type {typeName} from the given Assembly {assembly}");
            return CreateInstance<T>(toCreate, optionalConstructorParameters);
        }
    }
}
