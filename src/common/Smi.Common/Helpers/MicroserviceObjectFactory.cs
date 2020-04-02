﻿using System;
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
        public HostFatalHandler FatalHandler;

        /// <summary>
        /// Constructs an instance of the specified <see cref="toCreate"/> and casts it to Type T (e.g. an interface).  You can pass any 
        /// required or optional objects required for invoking the class constructor in via <see cref="optionalConstructorParameters"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toCreate"></param>
        /// <param name="optionalConstructorParameters"></param>
        /// <returns></returns>
        public T CreateInstance<T>(Type toCreate, params object[] optionalConstructorParameters)
        {
            T toReturn = default(T);

            try
            {
                var constructor = new ObjectConstructor();
                toReturn = (T)constructor.ConstructIfPossible(toCreate, optionalConstructorParameters);

                if (optionalConstructorParameters.Length > 0 && toReturn == null)
                    toReturn = (T)constructor.Construct(toCreate); // Try blank constructor

                if (toReturn == null)
                    throw new Exception("ConstructIfPossible returned null");

                _logger.Info("Successfully constructed Type '" + toReturn.GetType() + "'");
            }
            catch (Exception e)
            {
                _logger.Error(e,$"Failed to construct Type '{typeof(T)}'");

                if(FatalHandler != null)
                    FatalHandler(this,new FatalErrorEventArgs("Error constructing Type " + toCreate, e));
                else
                    throw;
            }

            return toReturn;
        }

        /// <summary>
        /// Constructs an instance of the specified <see cref="typeName"/> in the specified Assembly and casts it to Type T (e.g. an interface).
        /// You can pass any required or optional objects required for invoking the class constructor in via <see cref="optionalConstructorParameters"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeName"></param>
        /// <param name="assembly"></param>
        /// <param name="optionalConstructorParameters"></param>
        /// <returns></returns>
        public T CreateInstance<T>(string typeName, Assembly assembly, params object[] optionalConstructorParameters)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                _logger.Warn("No Type name specified for T " + typeof(T).Name);
                return default(T);
            }

            Type toCreate = assembly.GetType(typeName, true);
            return CreateInstance<T>(toCreate, optionalConstructorParameters);
        }
    }
}