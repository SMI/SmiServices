using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FAnsi.Discovery;
using Microservices.IsIdentifiable.Reporting;

namespace IsIdentifiableReviewer.Out
{
    public class RowUpdater
    {
        Dictionary<DiscoveredTable,DiscoveredColumn> _primaryKeys = new Dictionary<DiscoveredTable, DiscoveredColumn>();

        List<Failure> _committed = new List<Failure>();

        public void Update(Target target, Failure failure)
        {
            Update(target.Discover(),failure);
        }

        public void Update(DiscoveredServer server, Failure failure)
        {
            var syntax = server.GetQuerySyntaxHelper();

            //the fully specified name e.g. [mydb]..[mytbl]
            string tableName = failure.Resource;

            var tokens = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries);

            var db = tokens.First();
            tableName = tokens.Last();

            if(string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(tableName) || string.Equals(db , tableName))
                throw new NotSupportedException($"Could not understand table name {failure.Resource}, maybe it is not full specified with a valid database and table name?");

            db = syntax.GetRuntimeName(db);
            tableName = syntax.GetRuntimeName(tableName);

            DiscoveredTable table = server.ExpectDatabase(db).ExpectTable(tableName);

            //if we've never seen this table before
            if (!_primaryKeys.ContainsKey(table))
            {
                var pk = table.DiscoverColumns().SingleOrDefault(k => k.IsPrimaryKey);
                _primaryKeys.Add(table,pk);
            }

            using (var con = server.GetConnection())
            {
                con.Open();
                
                foreach (var part in failure.Parts)             
                {
                    string sql =
                        $@"update {table.GetFullyQualifiedName()} 
                SET {syntax.EnsureWrapped(failure.ProblemField)} = 
                REPLACE({syntax.EnsureWrapped(failure.ProblemField)},'{syntax.Escape(part.Word)}', 'SMI_REDACTED')
                WHERE {_primaryKeys[table].GetFullyQualifiedName()} = '{syntax.Escape(failure.ResourcePrimaryKey)}'";

                    var cmd = server.GetCommand(sql, con);
                    cmd.ExecuteNonQuery();
                }   
            }
            
            _committed.Add(failure);
        }

        /// <summary>
        /// Handler for loading <paramref name="failure"/>.  If the user previously made an update decision an
        /// update will transparently happen for this record and false is returned.
        /// </summary>
        /// <param name="currentTarget"></param>
        /// <param name="failure"></param>
        /// <returns>True if <paramref name="failure"/> is novel and not seen before</returns>
        public bool OnLoad(Target currentTarget,Failure failure)
        {
            //we have bigger problems than if this is novel!
            if (currentTarget == null)
                return true;

            //if we have seen this before
            if (_committed.Any(c => c.HaveSameProblem(failure)))
            {
                //since user has issued an update for this exact problem before we can update this one too
                Update(currentTarget,failure);

                //and return false to indicate that it is not a novel issue
                return false;
            }

            return true;
        }
    }
}
