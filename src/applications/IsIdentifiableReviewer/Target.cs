using FAnsi;
using FAnsi.Discovery;

namespace IsIdentifiableReviewer
{
    public class Target
    {
        public string Name {get;set;}
        public string ConnectionString { get; set; }
        public DatabaseType DatabaseType { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public DiscoveredServer Discover()
        {
            return new DiscoveredServer(ConnectionString, DatabaseType);
        }
    }
}