using FAnsi.Implementation;
using FAnsi.Implementations.MicrosoftSQL;
using FAnsi.Implementations.MySql;
using FAnsi.Implementations.PostgreSql;


namespace Smi.Common
{
    public static class FansiImplementations
    {
        public static void Load()
        {
            ImplementationManager.Load<PostgreSqlImplementation>();
            ImplementationManager.Load<MySqlImplementation>();
            ImplementationManager.Load<MicrosoftSQLImplementation>();
        }
    }
}
