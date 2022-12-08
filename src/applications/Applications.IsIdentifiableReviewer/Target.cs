using FAnsi;
using FAnsi.Discovery;
using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer
{
    /// <summary>
    /// The location of a database server for which <see cref="Failure"/> were detected and redaction may take place
    /// </summary>
    public class Target
    {
        /// <summary>
        /// The user friendly name of this database server
        /// </summary>
        public string Name {get;set;}

        /// <summary>
        /// Connection string for connecting to the server
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The DBMS type, MySql, Sql Server tec
        /// </summary>
        public DatabaseType DatabaseType { get; set; }

        /// <summary>
        /// Returns the <see cref="Name"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Returns a managed object for the <see cref="ConnectionString"/> for detecting tables, primary keys, running SQL statements etc
        /// </summary>
        /// <returns></returns>
        public DiscoveredServer Discover()
        {
            return new DiscoveredServer(ConnectionString, DatabaseType);
        }
    }
}
