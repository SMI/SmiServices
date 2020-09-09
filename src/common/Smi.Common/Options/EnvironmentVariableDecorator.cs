using System;
using System.Collections.Generic;
using System.Text;

namespace Smi.Common.Options
{

    /// <summary>
    /// Populates values in <see cref="GlobalOptions"/> based on environment variables 
    /// </summary>
    public class EnvironmentVariableDecorator : OptionsDecorator
    {
        public override GlobalOptions Decorate(GlobalOptions options)
        {
            ForAll<MongoDbOptions>(options,SetMongoPassword);
            
            var logsRoot = Environment.GetEnvironmentVariable("SMI_LOGS_ROOT");
            
            if(!string.IsNullOrWhiteSpace(logsRoot))
                options.LogsRoot = logsRoot;

            return options;
        }

        private MongoDbOptions SetMongoPassword(MongoDbOptions opt)
        {
            //get the environment variables current value
            var envVar = Environment.GetEnvironmentVariable("MONGO_SERVICE_PASSWORD");

            //if theres an env var for it and there are mongodb options being used
            if(!string.IsNullOrWhiteSpace(envVar) && opt != null)
                opt.Password = envVar;

            return opt;
        }
    }
}
