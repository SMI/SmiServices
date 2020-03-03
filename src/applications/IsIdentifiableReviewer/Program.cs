using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.Oracle;
using FAnsi.Implementations.PostgreSql;
using Terminal.Gui;
using YamlDotNet.Serialization;

namespace IsIdentifiableReviewer
{
    class Program
    {
        static int Main(string[] args)
        {
            ImplementationManager.Load<MySqlImplementation>();
            ImplementationManager.Load<OracleImplementation>();
            ImplementationManager.Load<MicrosoftSQLImplementation>();
            ImplementationManager.Load<PostgreSqlImplementation>();

            Deserializer d = new Deserializer();
            List<Target> targets;

            try
            {
                targets = d.Deserialize<List<Target>>(File.ReadAllText("Targets.yaml"));

                if (!targets.Any())
                    throw new Exception("No targets found in file");

            }
            catch (Exception e)
            {
                Console.WriteLine("Could not find/deserialize Targets.yaml");
                Console.WriteLine(e.ToString());
                return -1;
            }
            
            Application.Init();
            var mainWindow = new MainWindow(targets);
            Application.Top.Add(mainWindow);
            Application.Run();

            return 0;
        }
    }
}
