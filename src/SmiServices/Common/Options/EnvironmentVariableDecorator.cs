using System;

namespace SmiServices.Common.Options
{
    /// <summary>
    /// Populates values in <see cref="GlobalOptions"/> based on environment variables 
    /// </summary>
    public class EnvironmentVariableDecorator : OptionsDecorator
    {
        public override GlobalOptions Decorate(GlobalOptions options)
        {
            ForAll<MongoDbOptions>(options, SetMongoPassword);
            return options;
        }

        private MongoDbOptions SetMongoPassword(MongoDbOptions opt)
        {
            //get the environment variables current value
            string? envVar = Environment.GetEnvironmentVariable("MONGO_SERVICE_PASSWORD");

            //if there's an env var for it and there are mongodb options being used
            if (!string.IsNullOrWhiteSpace(envVar))
                opt.Password = envVar;

            return opt;
        }
    }
}
